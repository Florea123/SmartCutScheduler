using SmartCutScheduler.Domain.Entities;

namespace SmartCutScheduler.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    (string Token, string Hash, DateTime ExpiresAtUtc) GenerateRefreshToken();
    string Hash(string value);
}
