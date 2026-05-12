using System.Security.Claims;
using MediatR;
using SmartCutScheduler.Application.Features.BarberServices.GetMyServices;
using SmartCutScheduler.Application.Features.BarberServices.AddService;
using SmartCutScheduler.Application.Features.BarberServices.CreateCustomService;
using SmartCutScheduler.Application.Features.BarberServices.RemoveService;

namespace SmartCutScheduler.Api.Endpoints;

public static class BarberServicesEndpoints
{
    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId) =>
        Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId);

    public static void MapBarberServicesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/barbers/me/services")
            .WithTags("BarberServices")
            .RequireAuthorization(policy => policy.RequireRole("Barber"));

        group.MapGet("", async (ClaimsPrincipal user, IMediator mediator) =>
        {
            if (!TryGetUserId(user, out var userId))
                return Results.Unauthorized();
            return await mediator.Send(new GetMyServicesQuery(userId));
        });

        group.MapPost("", async (AddServiceRequest request, ClaimsPrincipal user, IMediator mediator) =>
        {
            if (!TryGetUserId(user, out var userId))
                return Results.Unauthorized();
            try
            {
                var serviceId = await mediator.Send(new AddServiceCommand(userId, request.ServiceId, request.CustomPrice));
                return Results.Ok(new { serviceId, message = "Serviciu adăugat cu succes!" });
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
        });

        group.MapPost("/custom", async (CreateCustomServiceRequest request, ClaimsPrincipal user, IMediator mediator) =>
        {
            if (!TryGetUserId(user, out var userId))
                return Results.Unauthorized();
            try
            {
                var serviceId = await mediator.Send(new CreateCustomServiceCommand(userId, request.Name, request.Description, request.DurationMinutes, request.Price));
                return Results.Ok(new { serviceId, message = "Serviciu creat cu succes!" });
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
        });

        group.MapDelete("/{serviceId:guid}", async (Guid serviceId, ClaimsPrincipal user, IMediator mediator) =>
        {
            if (!TryGetUserId(user, out var userId))
                return Results.Unauthorized();
            try
            {
                await mediator.Send(new RemoveServiceCommand(userId, serviceId));
                return Results.Ok(new { message = "Serviciu șters cu succes!" });
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
        });
    }
}

public record AddServiceRequest(Guid ServiceId, decimal? CustomPrice);
public record CreateCustomServiceRequest(string Name, string? Description, int DurationMinutes, decimal Price);
