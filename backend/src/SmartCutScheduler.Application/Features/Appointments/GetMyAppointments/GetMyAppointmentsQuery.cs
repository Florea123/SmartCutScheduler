using MediatR;
using Microsoft.AspNetCore.Http;

namespace SmartCutScheduler.Application.Features.Appointments.GetMyAppointments;

public record GetMyAppointmentsQuery : IRequest<IResult>;
