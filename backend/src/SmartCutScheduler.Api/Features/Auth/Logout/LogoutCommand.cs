using MediatR;

namespace SmartCutScheduler.Api.Features.Auth.Logout;

public record LogoutCommand : IRequest<IResult>;
