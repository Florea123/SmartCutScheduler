using MediatR;
using Microsoft.AspNetCore.Http;
using SmartCutScheduler.Application.Common.Interfaces;
using SmartCutScheduler.Application.Common.Models;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Enums;
using SmartCutScheduler.Domain.Repositories;

namespace SmartCutScheduler.Application.Features.Auth.Register;

public class RegisterCommandHandler(
    IUnitOfWork unitOfWork,
    IPasswordService passwordService,
    IJwtTokenService jwtTokenService,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<RegisterCommand, IResult>
{
    public async Task<IResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Role = UserRole.Customer
        };
        user.PasswordHash = passwordService.Hash(user, request.Password);

        await unitOfWork.Users.AddAsync(user, cancellationToken);

        var (rt, rtHash, expiresAt) = jwtTokenService.GenerateRefreshToken();
        await unitOfWork.RefreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = rtHash,
            ExpiresAtUtc = expiresAt
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var httpContext = httpContextAccessor.HttpContext!;
        httpContext.Response.Cookies.Append(
            "refresh_token",
            rt,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = expiresAt
            });

        var accessToken = jwtTokenService.GenerateAccessToken(user);
        var result = new AuthResultDto(
            accessToken,
            rt,
            DateTime.UtcNow.AddMinutes(15),
            expiresAt
        );

        return Results.Ok(result);
    }
}
