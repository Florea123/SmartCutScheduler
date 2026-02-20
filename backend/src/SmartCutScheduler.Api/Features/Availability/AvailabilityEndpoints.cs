using MediatR;
using SmartCutScheduler.Api.Features.Availability;

namespace SmartCutScheduler.Api.Features.Availability;

public static class AvailabilityEndpoints
{
    public static void MapAvailabilityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/availability").WithTags("Availability");

        group.MapGet("", async (Guid barberId, Guid serviceId, DateTime date, IMediator mediator) =>
            await mediator.Send(new GetAvailableSlotsQuery(barberId, serviceId, date)));
    }
}
