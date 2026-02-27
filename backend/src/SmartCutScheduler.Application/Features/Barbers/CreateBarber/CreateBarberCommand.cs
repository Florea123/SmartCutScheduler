using MediatR;

namespace SmartCutScheduler.Application.Features.Barbers.CreateBarber;

public record CreateBarberCommand(
    string Name,
    string Email,
    string PhoneNumber,
    string Password,
    string Description,
    string? PhotoUrl
) : IRequest<Guid>;
