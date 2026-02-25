using MediatR;
using SmartCutScheduler.Application.Features.Availability.GetAvailableSlots;

namespace SmartCutScheduler.Api.Endpoints;

public static class AvailabilityEndpoints
{
    public static void MapAvailabilityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/availability").WithTags("Availability");

        group.MapGet("", async (Guid barberId, Guid serviceId, DateTime date, IMediator mediator) =>
            await mediator.Send(new GetAvailableSlotsQuery(barberId, serviceId, date)));
    }
}
