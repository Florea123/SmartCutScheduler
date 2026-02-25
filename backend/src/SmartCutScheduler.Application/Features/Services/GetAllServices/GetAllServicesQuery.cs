using MediatR;
using Microsoft.AspNetCore.Http;

namespace SmartCutScheduler.Application.Features.Services.GetAllServices;

public record GetAllServicesQuery : IRequest<IResult>;
