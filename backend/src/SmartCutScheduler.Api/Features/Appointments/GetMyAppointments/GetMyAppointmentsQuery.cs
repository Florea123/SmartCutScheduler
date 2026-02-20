using MediatR;

namespace SmartCutScheduler.Api.Features.Appointments.GetMyAppointments;

public record GetMyAppointmentsQuery : IRequest<IResult>;
