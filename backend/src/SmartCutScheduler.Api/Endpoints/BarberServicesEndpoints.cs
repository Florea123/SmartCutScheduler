using System.Security.Claims;
using MediatR;
using SmartCutScheduler.Application.Features.BarberServices.GetMyServices;
using SmartCutScheduler.Application.Features.BarberServices.AddService;
using SmartCutScheduler.Application.Features.BarberServices.CreateCustomService;
using SmartCutScheduler.Application.Features.BarberServices.RemoveService;

namespace SmartCutScheduler.Api.Endpoints;

public static class BarberServicesEndpoints
{
    public static void MapBarberServicesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/barbers/me/services")
            .WithTags("BarberServices")
            .RequireAuthorization(policy => policy.RequireRole("Barber"));

        // GET /api/barbers/me/services - serviciile frizerului curent
        group.MapGet("", async (ClaimsPrincipal user, IMediator mediator) =>
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            // Presupunem că BarberId == UserId pentru useri cu rol Barber
            return await mediator.Send(new GetMyServicesQuery(userId));
        });

        // POST /api/barbers/me/services - adaugă serviciu
        group.MapPost("", async (AddServiceRequest request, ClaimsPrincipal user, IMediator mediator) =>
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            try
            {
                var serviceId = await mediator.Send(new AddServiceCommand(
                    userId, 
                    request.ServiceId, 
                    request.CustomPrice
                ));
                return Results.Ok(new { serviceId, message = "Serviciu adăugat cu succes!" });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        // POST /api/barbers/me/services/custom - creează serviciu custom
        group.MapPost("/custom", async (CreateCustomServiceRequest request, ClaimsPrincipal user, IMediator mediator) =>
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            try
            {
                var serviceId = await mediator.Send(new CreateCustomServiceCommand(
                    userId,
                    request.Name,
                    request.Description,
                    request.DurationMinutes,
                    request.Price
                ));
                return Results.Ok(new { serviceId, message = "Serviciu creat cu succes!" });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        // DELETE /api/barbers/me/services/{serviceId} - șterge serviciu
        group.MapDelete("/{serviceId:guid}", async (Guid serviceId, ClaimsPrincipal user, IMediator mediator) =>
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            try
            {
                await mediator.Send(new RemoveServiceCommand(userId, serviceId));
                return Results.Ok(new { message = "Serviciu șters cu succes!" });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });
    }
}

public record AddServiceRequest(Guid ServiceId, decimal? CustomPrice);
public record CreateCustomServiceRequest(string Name, string? Description, int DurationMinutes, decimal Price);
