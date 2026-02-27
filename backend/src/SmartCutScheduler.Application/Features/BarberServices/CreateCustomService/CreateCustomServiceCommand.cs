using MediatR;

namespace SmartCutScheduler.Application.Features.BarberServices.CreateCustomService;

public record CreateCustomServiceCommand(
    Guid BarberId,
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price
) : IRequest<Guid>;
