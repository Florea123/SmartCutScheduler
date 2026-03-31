using Xunit;
using FluentAssertions;
using SmartCutScheduler.Infrastructure.Auth;
using SmartCutScheduler.Domain.Entities;
using System;

public class JwtTokenService_AllCases
{
    [Fact]
    public void GenerateAccessToken_ShouldReturnToken()
    {
        var options = new JwtOptions { SigningKey = new string('a', 32), Issuer = "issuer", Audience = "aud", AccessTokenMinutes = 10, RefreshTokenDays = 1 };
        var service = new JwtTokenService(options);
        var user = new User { Id = Guid.NewGuid(), Name = "Test", Email = "test@test.com", Role = SmartCutScheduler.Domain.Enums.UserRole.Customer };
        var token = service.GenerateAccessToken(user);
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateAccessToken_ShouldContainCorrectClaims()
    {
        var options = new JwtOptions { SigningKey = new string('a', 32), Issuer = "issuer", Audience = "aud", AccessTokenMinutes = 10, RefreshTokenDays = 7 };
        var service = new JwtTokenService(options);
        var user = new User { Id = Guid.NewGuid(), Name = "Test", Email = "test@example.com", Role = SmartCutScheduler.Domain.Enums.UserRole.Customer };
        var token = service.GenerateAccessToken(user);
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        jwt.Claims.Should().Contain(c => c.Type == System.Security.Claims.ClaimTypes.Name && c.Value == "Test");
        jwt.Claims.Should().Contain(c => c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == "Customer");
        jwt.Claims.Should().Contain(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email && c.Value == "test@example.com");
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnTokenAndHash()
    {
        var options = new JwtOptions { SigningKey = new string('a', 32), Issuer = "issuer", Audience = "aud", AccessTokenMinutes = 10, RefreshTokenDays = 1 };
        var service = new JwtTokenService(options);
        var (token, hash, expires) = service.GenerateRefreshToken();
        token.Should().NotBeNullOrEmpty();
        hash.Should().NotBeNullOrEmpty();
        expires.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnUniqueTokens()
    {
        var options = new JwtOptions { SigningKey = new string('a', 32), Issuer = "issuer", Audience = "aud", AccessTokenMinutes = 10, RefreshTokenDays = 7 };
        var service = new JwtTokenService(options);
        var (token1, hash1, _) = service.GenerateRefreshToken();
        var (token2, hash2, _) = service.GenerateRefreshToken();
        token1.Should().NotBe(token2);
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Hash_ShouldReturnHash()
    {
        var options = new JwtOptions { SigningKey = new string('a', 32), Issuer = "issuer", Audience = "aud", AccessTokenMinutes = 10, RefreshTokenDays = 1 };
        var service = new JwtTokenService(options);
        var hash = service.Hash("test");
        hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Hash_ShouldBeDeterministic()
    {
        var options = new JwtOptions { SigningKey = new string('a', 32), Issuer = "issuer", Audience = "aud", AccessTokenMinutes = 10, RefreshTokenDays = 7 };
        var service = new JwtTokenService(options);
        var hash1 = service.Hash("value");
        var hash2 = service.Hash("value");
        hash1.Should().Be(hash2);
    }
}
