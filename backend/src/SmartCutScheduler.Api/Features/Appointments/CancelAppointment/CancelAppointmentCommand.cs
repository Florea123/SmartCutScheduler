using MediatR;

namespace SmartCutScheduler.Api.Features.Appointments.CancelAppointment;

public record CancelAppointmentCommand(Guid Id) : IRequest<IResult>;
