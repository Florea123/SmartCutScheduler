using MediatR;
using Microsoft.AspNetCore.Http;

namespace SmartCutScheduler.Application.Features.Barbers.GetAllBarbers;

public record GetAllBarbersQuery : IRequest<IResult>;
