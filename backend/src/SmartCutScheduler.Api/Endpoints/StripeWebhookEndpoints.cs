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

                // ...existing code...
                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                    if (session != null && session.Metadata != null)
                    {
                        var mediator = sp.GetRequiredService<MediatR.IMediator>();
                        string barberIdRaw = session.Metadata.ContainsKey("barberId") ? session.Metadata["barberId"] : null;
                        string serviceIdRaw = session.Metadata.ContainsKey("serviceId") ? session.Metadata["serviceId"] : null;
                        string dateRaw = session.Metadata.ContainsKey("date") ? session.Metadata["date"] : null;
                        string notesRaw = session.Metadata.ContainsKey("notes") ? session.Metadata["notes"] : null;
                        string userIdStr = session.Metadata.ContainsKey("userId") ? session.Metadata["userId"] : null;

                        Guid barberId, serviceId, userId;
                        DateTime date;
                        bool parseOk = true;
                        if (!Guid.TryParse(barberIdRaw, out barberId)) { parseOk = false; }
                        if (!Guid.TryParse(serviceIdRaw, out serviceId)) { parseOk = false; }
                        if (!DateTime.TryParse(dateRaw, out date)) { parseOk = false; }
                        if (!Guid.TryParse(userIdStr, out userId)) { parseOk = false; }

                        if (parseOk)
                        {
                            var cmd = new CreateAppointmentCommand(
                                barberId,
                                serviceId,
                                date.Date,
                                date.TimeOfDay.ToString("hh\\:mm"),
                                notesRaw
                            );
                            using (var scope = sp.CreateScope())
                            {
                                var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
                                var fakeContext = new DefaultHttpContext();
                                var claims = new List<System.Security.Claims.Claim>
                                {
                                    new(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString())
                                };
                                fakeContext.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(claims, "StripeWebhook"));
                                httpContextAccessor.HttpContext = fakeContext;
                                await mediator.Send(cmd);
                            }
                        }
                    }
                }
                return Results.Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Stripe webhook error: {ex}");
                return Results.Problem(ex.ToString());
            }
        });
    }
}
