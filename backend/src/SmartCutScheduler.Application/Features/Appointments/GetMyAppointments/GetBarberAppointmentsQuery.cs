using MediatR;
using Microsoft.AspNetCore.Http;

namespace SmartCutScheduler.Application.Features.Appointments.GetMyAppointments;

public record GetBarberAppointmentsQuery : IRequest<IResult>;
