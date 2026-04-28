using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartCutScheduler.Application.Features.Users.UpdateUserProfile;
using SmartCutScheduler.Application.Features.Users.UpdateFreshHaircutPhoto;

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
            var userId = GetUserId(http);
            if (userId == null) return Results.Unauthorized();
            var result = await mediator.Send(new SmartCutScheduler.Application.Features.Users.GetUserProfile.GetUserProfileQuery(Guid.Parse(userId)));
            return result != null ? Results.Ok(result) : Results.NotFound();
        });

        // ── Fresh haircut photo (used by the AI hair-check feature) ──────────
        group.MapPut("fresh-haircut-photo", async (HttpContext http, IMediator mediator) =>
        {
            var userIdStr = GetUserId(http);
            if (userIdStr == null) return Results.Unauthorized();

            var photoFile = await GetUploadedPhoto(http);
            if (photoFile == null) return Results.BadRequest("No photo uploaded.");

            var photoUrl = await mediator.Send(new UpdateFreshHaircutPhotoCommand
            {
                UserId = Guid.Parse(userIdStr),
                Photo = photoFile,
            });

            return Results.Ok(new { photoUrl });
        }).DisableAntiforgery();

        group.MapGet("fresh-haircut-photo", (HttpContext http, IMediator mediator) =>
            HandleGetFreshHaircutPhoto(http, mediator));
    }

    private static async Task<IResult> HandleGetFreshHaircutPhoto(HttpContext http, IMediator mediator)
    {
        var userIdStr = GetUserId(http);
        if (userIdStr == null) return Results.Unauthorized();

        var profile = await mediator.Send(
            new SmartCutScheduler.Application.Features.Users.GetUserProfile.GetUserProfileQuery(
                Guid.Parse(userIdStr)));

        if (profile == null) return Results.NotFound();

        return string.IsNullOrWhiteSpace(profile.FreshHaircutPhotoUrl)
            ? Results.Ok(new { photoUrl = (string?)null })
            : Results.Ok(new { photoUrl = profile.FreshHaircutPhotoUrl });
    }

    private static string? GetUserId(HttpContext http) =>
        http.User.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type.EndsWith("nameidentifier"))?.Value;

    private static async Task<IFormFile?> GetUploadedPhoto(HttpContext http)
    {
        if (!http.Request.HasFormContentType) return null;
        var form = await http.Request.ReadFormAsync();
        var photo = form.Files.GetFile("photo");
        return (photo == null || photo.Length == 0) ? null : photo;
    }
}