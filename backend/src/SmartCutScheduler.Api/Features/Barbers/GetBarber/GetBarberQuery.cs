using MediatR;

namespace SmartCutScheduler.Api.Features.Barbers.GetBarber;

public record GetBarberQuery(Guid Id) : IRequest<IResult>;
