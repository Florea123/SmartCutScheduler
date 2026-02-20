using MediatR;
using SmartCutScheduler.Api.Features.Auth.Login;
using SmartCutScheduler.Api.Features.Auth.Logout;
using SmartCutScheduler.Api.Features.Auth.Refresh;
using SmartCutScheduler.Api.Features.Auth.Register;

namespace SmartCutScheduler.Api.Features.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (RegisterUserCommand cmd, IMediator mediator) =>
            await mediator.Send(cmd));

        group.MapPost("/login", async (LoginUserCommand cmd, IMediator mediator) =>
            await mediator.Send(cmd));

        group.MapPost("/logout", async (IMediator mediator) =>
            await mediator.Send(new LogoutCommand()));

        group.MapPost("/refresh", async (IMediator mediator) =>
            await mediator.Send(new RefreshCommand()));
    }
}
