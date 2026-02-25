using MediatR;
using Microsoft.AspNetCore.Http;

namespace SmartCutScheduler.Application.Features.Auth.Register;

public record RegisterCommand(
    string Name,
    string Email,
    string Password,
    string? PhoneNumber
) : IRequest<IResult>;
