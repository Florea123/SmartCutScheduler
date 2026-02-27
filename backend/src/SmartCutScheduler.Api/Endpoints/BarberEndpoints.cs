using MediatR;
using SmartCutScheduler.Application.Features.Barbers.GetAllBarbers;
using SmartCutScheduler.Application.Features.Barbers.GetBarber;
using SmartCutScheduler.Application.Features.Barbers.GetBarberWorkSchedule;
using SmartCutScheduler.Application.Features.Barbers.CreateBarber;
using SmartCutScheduler.Application.Features.Barbers.DeleteBarber;
using SmartCutScheduler.Application.Features.Availability.GetDaySlots;

namespace SmartCutScheduler.Api.Endpoints;

public static class BarberEndpoints
{
    public static void MapBarberEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/barbers").WithTags("Barbers");

        group.MapGet("", async (IMediator mediator) =>
            await mediator.Send(new GetAllBarbersQuery()));

        group.MapPost("", async (CreateBarberCommand cmd, IMediator mediator) =>
        {
            try
            {
                var barberId = await mediator.Send(cmd);
                return Results.Ok(new { id = barberId, message = "Frizer creat cu succes!" });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
            await mediator.Send(new GetBarberQuery(id)));

        group.MapGet("/{id:guid}/schedule", async (Guid id, IMediator mediator) =>
            await mediator.Send(new GetBarberWorkScheduleQuery(id)));

        group.MapGet("/{id:guid}/test", () => Results.Ok(new { message = "Test endpoint works!" }));

        group.MapGet("/{id:guid}/day-slots", async (Guid id, DateTime date, IMediator mediator) =>
            await mediator.Send(new GetDaySlotsQuery(id, date)));

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
            await mediator.Send(new DeleteBarberCommand(id)))
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
