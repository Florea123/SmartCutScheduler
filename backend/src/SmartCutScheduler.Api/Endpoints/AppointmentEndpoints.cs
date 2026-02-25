using MediatR;
using SmartCutScheduler.Application.Features.Appointments.CancelAppointment;
using SmartCutScheduler.Application.Features.Appointments.CreateAppointment;
using SmartCutScheduler.Application.Features.Appointments.GetMyAppointments;

namespace SmartCutScheduler.Api.Endpoints;

public static class AppointmentEndpoints
{
    public static void MapAppointmentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/appointments")
            .WithTags("Appointments")
            .RequireAuthorization();

        group.MapPost("", async (CreateAppointmentCommand cmd, IMediator mediator) =>
            await mediator.Send(cmd));

        group.MapGet("/my", async (IMediator mediator) =>
            await mediator.Send(new GetMyAppointmentsQuery()));

        group.MapPut("/{id:guid}/cancel", async (Guid id, IMediator mediator) =>
            await mediator.Send(new CancelAppointmentCommand(id)));
    }
}
