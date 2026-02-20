using MediatR;
using SmartCutScheduler.Api.Features.Barbers.GetAllBarbers;
using SmartCutScheduler.Api.Features.Barbers.GetBarber;

namespace SmartCutScheduler.Api.Features.Barbers;

public static class BarberEndpoints
{
    public static void MapBarberEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/barbers").WithTags("Barbers");

        group.MapGet("", async (IMediator mediator) =>
            await mediator.Send(new GetAllBarbersQuery()));

        group.MapGet("{id:guid}", async (Guid id, IMediator mediator) =>
            await mediator.Send(new GetBarberQuery(id)));
    }
}
