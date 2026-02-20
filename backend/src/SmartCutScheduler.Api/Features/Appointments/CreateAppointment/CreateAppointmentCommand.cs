using MediatR;

namespace SmartCutScheduler.Api.Features.Appointments.CreateAppointment;

public record CreateAppointmentCommand(
    Guid BarberId,
    Guid ServiceId,
    DateTime AppointmentDate,
    string StartTime,
    string? Notes
) : IRequest<IResult>;
