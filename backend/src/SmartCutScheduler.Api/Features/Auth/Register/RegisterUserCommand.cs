using SmartCutScheduler.Api.Enums;
using MediatR;

namespace SmartCutScheduler.Api.Features.Auth.Register;

public record RegisterUserCommand(
    string Name,
    string Email,
    string Password,
    string? PhoneNumber
) : IRequest<IResult>;
