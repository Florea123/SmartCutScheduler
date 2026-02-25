using MediatR;
using Microsoft.AspNetCore.Http;

namespace SmartCutScheduler.Application.Features.Availability.GetAvailableSlots;

public record GetAvailableSlotsQuery(
    Guid BarberId,
    Guid ServiceId,
    DateTime Date
) : IRequest<IResult>;
