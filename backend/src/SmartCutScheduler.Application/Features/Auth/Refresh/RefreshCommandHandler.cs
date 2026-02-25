using MediatR;
using Microsoft.AspNetCore.Http;
using SmartCutScheduler.Application.Common.Interfaces;
using SmartCutScheduler.Application.Common.Models;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Repositories;

namespace SmartCutScheduler.Application.Features.Auth.Refresh;

public class RefreshCommandHandler(
    IUnitOfWork unitOfWork,
    IJwtTokenService jwtTokenService,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<RefreshCommand, IResult>
{
    public async Task<IResult> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        var ctx = httpContextAccessor.HttpContext!;
        
        if (!ctx.Request.Cookies.TryGetValue("refresh_token", out var token))
            return Results.Unauthorized();

        var hash = jwtTokenService.Hash(token);
        var refreshToken = await unitOfWork.RefreshTokens.GetByTokenAsync(hash, cancellationToken);

        if (refreshToken is null || refreshToken.ExpiresAtUtc < DateTime.UtcNow)
            return Results.Unauthorized();

        var user = await unitOfWork.Users.GetByIdAsync(refreshToken.UserId, cancellationToken);
        if (user is null)
            return Results.Unauthorized();

        // Generate new tokens
        var (newRt, newRtHash, expiresAt) = jwtTokenService.GenerateRefreshToken();
        
        // Delete old token and add new one
        await unitOfWork.RefreshTokens.DeleteAsync(refreshToken.Id, cancellationToken);
        await unitOfWork.RefreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = newRtHash,
            ExpiresAtUtc = expiresAt
        }, cancellationToken);
        
        await unitOfWork.SaveChangesAsync(cancellationToken);

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

        var accessToken = jwtTokenService.GenerateAccessToken(user);
        return Results.Ok(new AuthResultDto(accessToken, newRt, DateTime.UtcNow.AddMinutes(15), expiresAt));
    }
}
