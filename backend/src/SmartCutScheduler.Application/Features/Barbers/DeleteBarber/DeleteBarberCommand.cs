using MediatR;
using Microsoft.AspNetCore.Http;

namespace SmartCutScheduler.Application.Features.Barbers.DeleteBarber;

public record DeleteBarberCommand(Guid Id) : IRequest<IResult>;
