using System.Net;
using FluentValidation;

namespace SmartCutScheduler.Api.Infrastructure.Validation;

public sealed class ValidationExceptionMiddleware(RequestDelegate next, ILogger<ValidationExceptionMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            logger.LogInformation("Validation failed: {Errors}",
                string.Join("; ", ex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";

            var errorMessages = ex.Errors
                .Select(e => e.ErrorMessage)
                .Distinct()
                .ToArray();

            await context.Response.WriteAsJsonAsync(new
            {
                message = "Validation failed.",
                errors = errorMessages
            });
        }
    }
}
