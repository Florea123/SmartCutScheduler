using SmartCutScheduler.Api.Data;
using SmartCutScheduler.Api.Infrastructure.Auth;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace SmartCutScheduler.Api.Features.Auth.Logout;

public class LogoutHandler(
    AppDbContext db,
    IJwtTokenService jwt,
    IHttpContextAccessor http
) : IRequestHandler<LogoutCommand, IResult>
{
    public async Task<IResult> Handle(LogoutCommand request, CancellationToken ct)
    {
        var ctx = http.HttpContext!;
        
        if (!ctx.Request.Cookies.TryGetValue("refresh_token", out var token))
            return Results.BadRequest("No refresh token found.");

        var hash = jwt.Hash(token);
        var refreshToken = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == hash, ct);

        if (refreshToken is not null)
        {
            db.RefreshTokens.Remove(refreshToken);
            await db.SaveChangesAsync(ct);
        }

        ctx.Response.Cookies.Delete("refresh_token");
        return Results.Ok("Logged out successfully.");
    }
}
