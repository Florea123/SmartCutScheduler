using MediatR;
using Microsoft.AspNetCore.Http;

namespace SmartCutScheduler.Application.Features.Users.DeleteUser;

public record DeleteUserCommand(Guid Id) : IRequest<IResult>;
