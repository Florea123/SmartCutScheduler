using MediatR;
using SmartCutScheduler.Application.Features.Services.GetAllServices;

namespace SmartCutScheduler.Api.Endpoints;

public static class ServiceEndpoints
{
    public static void MapServiceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/services").WithTags("Services");

        group.MapGet("", async (IMediator mediator) =>
            await mediator.Send(new GetAllServicesQuery()));
    }
}
