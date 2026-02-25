using MediatR;
using Microsoft.AspNetCore.Http;

namespace SmartCutScheduler.Application.Features.Auth.Logout;

public record LogoutCommand : IRequest<IResult>;
