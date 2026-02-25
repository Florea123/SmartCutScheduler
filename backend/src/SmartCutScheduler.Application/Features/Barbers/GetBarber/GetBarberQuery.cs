using MediatR;
using Microsoft.AspNetCore.Http;

namespace SmartCutScheduler.Application.Features.Barbers.GetBarber;

public record GetBarberQuery(Guid Id) : IRequest<IResult>;
