using SmartCutScheduler.Api.Data;
using SmartCutScheduler.Api.Domain;
using SmartCutScheduler.Api.Enums;
using SmartCutScheduler.Api.Infrastructure.Auth;
using SmartCutScheduler.Api.Infrastructure.Security;
using MediatR;

namespace SmartCutScheduler.Api.Features.Auth.Register;

public class RegisterUserHandler(
    AppDbContext db,
    IPasswordService passwords,
    IJwtTokenService jwt,
    IHttpContextAccessor http
) : IRequestHandler<RegisterUserCommand, IResult>
{
    public async Task<IResult> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Role = UserRole.Customer
        };
        user.PasswordHash = passwords.Hash(user, request.Password);

        db.Users.Add(user);

        var (rt, rtHash, expiresAt) = jwt.GenerateRefreshToken();
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = rtHash,
            ExpiresAtUtc = expiresAt
        });

        await db.SaveChangesAsync(ct);

        var ctxResponse = http.HttpContext!;
        ctxResponse.Response.Cookies.Append(
            "refresh_token",
            rt,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = expiresAt
            });

        var accessToken = jwt.GenerateAccessToken(user);
        var result = new AuthResultDto(
            accessToken,
            rt,
            DateTime.UtcNow.AddMinutes(15),
            expiresAt
        );

        return Results.Ok(result);
    }
}
