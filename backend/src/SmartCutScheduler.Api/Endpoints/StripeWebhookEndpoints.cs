using Microsoft.AspNetCore.Mvc;
using Stripe;
using SmartCutScheduler.Application.Features.Appointments.CreateAppointment;

namespace SmartCutScheduler.Api.Endpoints;

public static class StripeWebhookEndpoints
{
    public static void MapStripeWebhookEndpoints(this WebApplication app)
    {
        app.MapPost("/api/stripe/webhook", async ([FromServices] IServiceProvider sp, HttpRequest request) =>
        {
            try
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var json = await new StreamReader(request.Body).ReadToEndAsync();
                var stripeEvent = EventUtility.ConstructEvent(json, request.Headers["Stripe-Signature"], config["Stripe:WebhookSecret"]);

                if (stripeEvent.Type == "checkout.session.completed")
                    await HandleCheckoutCompletedAsync(sp, stripeEvent);

                return Results.Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Stripe webhook error: {ex}");
                return Results.Problem(ex.ToString());
            }
        });
    }

    private static async Task HandleCheckoutCompletedAsync(IServiceProvider sp, Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
        if (session?.Metadata == null)
            return;

        if (!TryParseSessionMetadata(session.Metadata, out var barberId, out var serviceId, out var userId, out var date, out var notesRaw))
            return;

        var cmd = new CreateAppointmentCommand(
            barberId,
            serviceId,
            date.Date,
            date.TimeOfDay.ToString("hh\\:mm"),
            notesRaw
        );

        var mediator = sp.GetRequiredService<MediatR.IMediator>();
        using var scope = sp.CreateScope();
        var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        var fakeContext = new DefaultHttpContext();
        fakeContext.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString())],
                "StripeWebhook"));
        httpContextAccessor.HttpContext = fakeContext;
        await mediator.Send(cmd);
    }

    private static bool TryParseSessionMetadata(
        IDictionary<string, string> metadata,
        out Guid barberId, out Guid serviceId, out Guid userId,
        out DateTime date, out string? notes)
    {
        barberId = serviceId = userId = Guid.Empty;
        date = default;
        metadata.TryGetValue("notes", out notes);

        metadata.TryGetValue("barberId", out var barberIdRaw);
        metadata.TryGetValue("serviceId", out var serviceIdRaw);
        metadata.TryGetValue("userId", out var userIdRaw);
        metadata.TryGetValue("date", out var dateRaw);

        return Guid.TryParse(barberIdRaw, out barberId)
            && Guid.TryParse(serviceIdRaw, out serviceId)
            && Guid.TryParse(userIdRaw, out userId)
            && DateTime.TryParse(dateRaw, System.Globalization.CultureInfo.InvariantCulture, out date);
    }
}
