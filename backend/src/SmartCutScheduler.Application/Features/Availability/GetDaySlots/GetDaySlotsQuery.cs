using MediatR;
using Microsoft.AspNetCore.Http;

namespace SmartCutScheduler.Application.Features.Availability.GetDaySlots;

public record GetDaySlotsQuery(
    Guid BarberId,
    DateTime Date
) : IRequest<IResult>;
