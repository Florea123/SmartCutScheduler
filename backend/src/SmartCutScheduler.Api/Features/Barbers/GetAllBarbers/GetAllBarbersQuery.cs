using MediatR;

namespace SmartCutScheduler.Api.Features.Barbers.GetAllBarbers;

public record GetAllBarbersQuery : IRequest<IResult>;
