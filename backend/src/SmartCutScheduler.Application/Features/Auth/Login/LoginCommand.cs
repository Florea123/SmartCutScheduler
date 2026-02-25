using MediatR;
using Microsoft.AspNetCore.Http;

namespace SmartCutScheduler.Application.Features.Auth.Login;

public record LoginCommand(
    string Email,
    string Password
) : IRequest<IResult>;
