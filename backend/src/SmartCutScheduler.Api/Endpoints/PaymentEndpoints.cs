using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace SmartCutScheduler.Api.Endpoints;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/payments").WithTags("Payments").RequireAuthorization(policy => policy.RequireRole("Customer"));

        group.MapPost("/create-checkout-session", async (HttpContext ctx, [FromBody] CreateCheckoutSessionRequest req) =>
        {
            var stripeKey = app.Configuration["Stripe:SecretKey"];
            if (string.IsNullOrEmpty(stripeKey))
                return Results.Problem("Payment service is not configured.");
            StripeConfiguration.ApiKey = stripeKey;

            var userIdClaim = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userId = userIdClaim ?? string.Empty;

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "ron",
                            UnitAmount = (long)(req.Amount * 100),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = req.ServiceName
                            },
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = app.Configuration["Stripe:SuccessUrl"] ?? req.SuccessUrl,
                CancelUrl = app.Configuration["Stripe:CancelUrl"] ?? req.CancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    { "barberId", req.BarberId.ToString() },
                    { "serviceId", req.ServiceId.ToString() },
                    { "date", req.Date.ToString("o") },
                    { "notes", req.Notes ?? string.Empty },
                    { "userId", userId }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);
            return Results.Ok(new { sessionId = session.Id, url = session.Url });
        });
    }

    public class CreateCheckoutSessionRequest
    {
        public Guid BarberId { get; set; }
        public Guid ServiceId { get; set; }
        public DateTime Date { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Notes { get; set; }
        public string? SuccessUrl { get; set; }
        public string? CancelUrl { get; set; }
    }
}
