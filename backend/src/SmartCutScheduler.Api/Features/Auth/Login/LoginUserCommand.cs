using MediatR;

namespace SmartCutScheduler.Api.Features.Auth.Login;

public record LoginUserCommand(
    string Email,
    string Password
) : IRequest<IResult>;
