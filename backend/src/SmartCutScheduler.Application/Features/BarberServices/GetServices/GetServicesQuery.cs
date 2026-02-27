using MediatR;
using Microsoft.AspNetCore.Http;

namespace SmartCutScheduler.Application.Features.BarberServices.GetServices;

public record GetServicesQuery : IRequest<IResult>;
