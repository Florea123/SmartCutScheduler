using MediatR;
using Microsoft.AspNetCore.Http;
using SmartCutScheduler.Application.Common.Interfaces;
using SmartCutScheduler.Application.Common.Models;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Repositories;

namespace SmartCutScheduler.Application.Features.Auth.Login;

public class LoginCommandHandler(
    IUnitOfWork unitOfWork,
    IPasswordService passwordService,
    IJwtTokenService jwtTokenService,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<LoginCommand, IResult>
{
    public async Task<IResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !passwordService.Verify(user, user.PasswordHash, request.Password))
            return Results.BadRequest("Invalid email or password.");

        var (rt, rtHash, expiresAt) = jwtTokenService.GenerateRefreshToken();
        
        // Revoke old refresh tokens
        await unitOfWork.RefreshTokens.DeleteByUserIdAsync(user.Id, cancellationToken);
        
        await unitOfWork.RefreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = rtHash,
            ExpiresAtUtc = expiresAt
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        httpContextAccessor.HttpContext!.Response.Cookies.Append(
            "refresh_token",
            rt,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = expiresAt
            });

        var access = jwtTokenService.GenerateAccessToken(user);
        return Results.Ok(new AuthResultDto(access, rt, DateTime.UtcNow.AddMinutes(15), expiresAt));
    }
}
