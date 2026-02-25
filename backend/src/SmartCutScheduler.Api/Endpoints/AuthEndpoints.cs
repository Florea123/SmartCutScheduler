using MediatR;
using SmartCutScheduler.Application.Features.Auth.Login;
using SmartCutScheduler.Application.Features.Auth.Logout;
using SmartCutScheduler.Application.Features.Auth.Refresh;
using SmartCutScheduler.Application.Features.Auth.Register;

namespace SmartCutScheduler.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (RegisterCommand cmd, IMediator mediator) =>
            await mediator.Send(cmd));

        group.MapPost("/login", async (LoginCommand cmd, IMediator mediator) =>
            await mediator.Send(cmd));

        group.MapPost("/logout", async (IMediator mediator) =>
            await mediator.Send(new LogoutCommand()));

        group.MapPost("/refresh", async (IMediator mediator) =>
            await mediator.Send(new RefreshCommand()));
    }
}
