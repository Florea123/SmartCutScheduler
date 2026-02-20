using MediatR;

namespace SmartCutScheduler.Api.Features.Availability;

public record GetAvailableSlotsQuery(
    Guid BarberId,
    Guid ServiceId,
    DateTime Date
) : IRequest<IResult>;
