using MediatR;
using Microsoft.AspNetCore.Http;

namespace SmartCutScheduler.Application.Features.Appointments.CreateAppointment;

public record CreateAppointmentCommand(
    Guid BarberId,
    Guid ServiceId,
    DateTime AppointmentDate,
    string StartTime,
    string? Notes
) : IRequest<IResult>;
