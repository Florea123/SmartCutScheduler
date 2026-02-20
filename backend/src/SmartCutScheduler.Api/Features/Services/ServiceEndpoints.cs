using MediatR;
using SmartCutScheduler.Api.Features.Services.GetAllServices;

namespace SmartCutScheduler.Api.Features.Services;

public static class ServiceEndpoints
{
    public static void MapServiceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/services").WithTags("Services");

        group.MapGet("", async (IMediator mediator) =>
            await mediator.Send(new GetAllServicesQuery()));
    }
}
