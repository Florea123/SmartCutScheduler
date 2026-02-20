using MediatR;

namespace SmartCutScheduler.Api.Features.Services.GetAllServices;

public record GetAllServicesQuery : IRequest<IResult>;
