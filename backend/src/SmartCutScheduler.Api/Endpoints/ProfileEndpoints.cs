using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartCutScheduler.Application.Features.Users.UpdateUserProfile;

namespace SmartCutScheduler.Api.Endpoints;

public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profile").RequireAuthorization();

        group.MapPut("", async ([FromForm] UpdateUserProfileCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return result ? Results.Ok() : Results.BadRequest();
        }).DisableAntiforgery();

        group.MapGet("", async (HttpContext http, IMediator mediator) =>
        {
            var userId = http.User.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type.EndsWith("nameidentifier"))?.Value;
            if (userId == null) return Results.Unauthorized();
            var result = await mediator.Send(new SmartCutScheduler.Application.Features.Users.GetUserProfile.GetUserProfileQuery(Guid.Parse(userId)));
            return result != null ? Results.Ok(result) : Results.NotFound();
        });
    }
}