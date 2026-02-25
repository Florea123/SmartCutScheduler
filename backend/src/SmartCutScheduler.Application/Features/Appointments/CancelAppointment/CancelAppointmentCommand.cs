using MediatR;
using Microsoft.AspNetCore.Http;

namespace SmartCutScheduler.Application.Features.Appointments.CancelAppointment;

public record CancelAppointmentCommand(Guid Id) : IRequest<IResult>;
