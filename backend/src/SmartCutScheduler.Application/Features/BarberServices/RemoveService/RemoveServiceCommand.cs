using MediatR;

namespace SmartCutScheduler.Application.Features.BarberServices.RemoveService;

public record RemoveServiceCommand(
    Guid BarberId,
    Guid ServiceId
) : IRequest<bool>;
