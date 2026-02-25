using MediatR;
using Microsoft.AspNetCore.Http;
using SmartCutScheduler.Application.Common.Interfaces;
using SmartCutScheduler.Domain.Repositories;

namespace SmartCutScheduler.Application.Features.Auth.Logout;

public class LogoutCommandHandler(
    IUnitOfWork unitOfWork,
    IJwtTokenService jwtTokenService,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<LogoutCommand, IResult>
{
    public async Task<IResult> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var ctx = httpContextAccessor.HttpContext!;
        
        if (!ctx.Request.Cookies.TryGetValue("refresh_token", out var token))
            return Results.BadRequest("No refresh token found.");

        var hash = jwtTokenService.Hash(token);
        var refreshToken = await unitOfWork.RefreshTokens.GetByTokenAsync(hash, cancellationToken);

        if (refreshToken is not null)
        {
            await unitOfWork.RefreshTokens.DeleteAsync(refreshToken.Id, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        ctx.Response.Cookies.Delete("refresh_token");
        return Results.Ok("Logged out successfully.");
    }
}
