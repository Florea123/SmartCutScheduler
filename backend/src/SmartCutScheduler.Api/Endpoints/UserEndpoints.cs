using MediatR;
using SmartCutScheduler.Application.Features.Users.GetAllUsers;
using SmartCutScheduler.Application.Features.Users.DeleteUser;

namespace SmartCutScheduler.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapGet("", async (IMediator mediator) =>
            await mediator.Send(new GetAllUsersQuery()));

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
            await mediator.Send(new DeleteUserCommand(id)));
    }
}
