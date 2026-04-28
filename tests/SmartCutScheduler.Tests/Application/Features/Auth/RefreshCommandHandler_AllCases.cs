using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using SmartCutScheduler.Application.Common.Interfaces;
using SmartCutScheduler.Application.Features.Auth.Refresh;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Repositories;
using Xunit;

namespace SmartCutScheduler.Tests.Application.Features.Auth;

public class RefreshCommandHandler_AllCases
{
    private static RefreshCommandHandler CreateHandler(
        HttpContext httpContext,
        Mock<IUnitOfWork> unitOfWorkMock,
        Mock<IJwtTokenService> jwtMock)
    {
        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        return new RefreshCommandHandler(unitOfWorkMock.Object, jwtMock.Object, accessorMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenNoCookie()
    {
        var httpContext = new DefaultHttpContext();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var jwtMock = new Mock<IJwtTokenService>();
        var handler = CreateHandler(httpContext, unitOfWorkMock, jwtMock);

        var result = await handler.Handle(new RefreshCommand(), CancellationToken.None);

        result.ToString().Should().Contain("Unauthorized");
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenTokenNotFound()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Cookie = "refresh_token=sometoken";

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var jwtMock = new Mock<IJwtTokenService>();
        jwtMock.Setup(j => j.Hash("sometoken")).Returns("hashval");
        unitOfWorkMock.Setup(u => u.RefreshTokens.GetByTokenAsync("hashval", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var handler = CreateHandler(httpContext, unitOfWorkMock, jwtMock);
        var result = await handler.Handle(new RefreshCommand(), CancellationToken.None);

        result.ToString().Should().Contain("Unauthorized");
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenTokenExpired()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Cookie = "refresh_token=expiredtoken";

        var expiredToken = new RefreshToken { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), ExpiresAtUtc = DateTime.UtcNow.AddDays(-1) };
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var jwtMock = new Mock<IJwtTokenService>();
        jwtMock.Setup(j => j.Hash("expiredtoken")).Returns("expiredhash");
        unitOfWorkMock.Setup(u => u.RefreshTokens.GetByTokenAsync("expiredhash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredToken);

        var handler = CreateHandler(httpContext, unitOfWorkMock, jwtMock);
        var result = await handler.Handle(new RefreshCommand(), CancellationToken.None);

        result.ToString().Should().Contain("Unauthorized");
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserNotFound()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Cookie = "refresh_token=validtoken";

        var refreshToken = new RefreshToken { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), ExpiresAtUtc = DateTime.UtcNow.AddDays(1) };
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var jwtMock = new Mock<IJwtTokenService>();
        jwtMock.Setup(j => j.Hash("validtoken")).Returns("validhash");
        unitOfWorkMock.Setup(u => u.RefreshTokens.GetByTokenAsync("validhash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);
        unitOfWorkMock.Setup(u => u.Users.GetByIdAsync(refreshToken.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = CreateHandler(httpContext, unitOfWorkMock, jwtMock);
        var result = await handler.Handle(new RefreshCommand(), CancellationToken.None);

        result.ToString().Should().Contain("Unauthorized");
    }

    [Fact]
    public async Task Handle_ShouldReturnOk_WhenTokenIsValid()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Cookie = "refresh_token=goodtoken";

        var userId = Guid.NewGuid();
        var refreshToken = new RefreshToken { Id = Guid.NewGuid(), UserId = userId, ExpiresAtUtc = DateTime.UtcNow.AddDays(7) };
        var user = new User { Id = userId, Name = "Test", Email = "t@t.com" };

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var jwtMock = new Mock<IJwtTokenService>();
        jwtMock.Setup(j => j.Hash("goodtoken")).Returns("goodhash");
        unitOfWorkMock.Setup(u => u.RefreshTokens.GetByTokenAsync("goodhash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);
        unitOfWorkMock.Setup(u => u.Users.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        jwtMock.Setup(j => j.GenerateRefreshToken()).Returns(("newrt", "newrthash", DateTime.UtcNow.AddDays(7)));
        jwtMock.Setup(j => j.GenerateAccessToken(user)).Returns("newaccesstoken");
        unitOfWorkMock.Setup(u => u.RefreshTokens.DeleteAsync(refreshToken.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        unitOfWorkMock.Setup(u => u.RefreshTokens.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = CreateHandler(httpContext, unitOfWorkMock, jwtMock);
        var result = await handler.Handle(new RefreshCommand(), CancellationToken.None);

        result.ToString().Should().Contain("Ok");
    }
}
