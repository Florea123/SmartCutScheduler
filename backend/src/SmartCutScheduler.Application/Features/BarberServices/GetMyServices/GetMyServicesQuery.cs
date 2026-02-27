using MediatR;
using Microsoft.AspNetCore.Http;

namespace SmartCutScheduler.Application.Features.BarberServices.GetMyServices;

public record GetMyServicesQuery(Guid BarberId) : IRequest<IResult>;
