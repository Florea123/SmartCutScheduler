using SmartCutScheduler.Api.Data;
using SmartCutScheduler.Api.Infrastructure.Auth;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace SmartCutScheduler.Api.Features.Auth.Refresh;

public class RefreshHandler(
    AppDbContext db,
    IJwtTokenService jwt,
    IHttpContextAccessor http
) : IRequestHandler<RefreshCommand, IResult>
{
    public async Task<IResult> Handle(RefreshCommand request, CancellationToken ct)
    {
        var ctx = http.HttpContext!;
        
        if (!ctx.Request.Cookies.TryGetValue("refresh_token", out var token))
            return Results.Unauthorized();

        var hash = jwt.Hash(token);
        var refreshToken = await db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == hash, ct);

        if (refreshToken is null || refreshToken.ExpiresAtUtc < DateTime.UtcNow)
            return Results.Unauthorized();

        // Generate new tokens
        var (newRt, newRtHash, expiresAt) = jwt.GenerateRefreshToken();
        
        db.RefreshTokens.Remove(refreshToken);
        db.RefreshTokens.Add(new Domain.RefreshToken
        {
            UserId = refreshToken.UserId,
            Token = newRtHash,
            ExpiresAtUtc = expiresAt
        });

        await db.SaveChangesAsync(ct);

        ctx.Response.Cookies.Append(
            "refresh_token",
            newRt,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = expiresAt
            });

        var access = jwt.GenerateAccessToken(refreshToken.User);
        return Results.Ok(new AuthResultDto(access, newRt, DateTime.UtcNow.AddMinutes(15), expiresAt));
    }
}
