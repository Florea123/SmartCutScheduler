using MediatR;

namespace SmartCutScheduler.Application.Features.BarberServices.AddService;

public record AddServiceCommand(
    Guid BarberId,
    Guid ServiceId,
    decimal? CustomPrice
) : IRequest<Guid>;
