using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using SmartCutScheduler.Application.Common.Interfaces;
using SmartCutScheduler.Application.Features.Auth.Logout;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Repositories;
using Xunit;

namespace SmartCutScheduler.Tests.Application.Features.Auth;

public class LogoutCommandHandler_AllCases
{
    private static LogoutCommandHandler CreateHandler(
        HttpContext httpContext,
        Mock<IUnitOfWork> unitOfWorkMock,
        Mock<IJwtTokenService> jwtMock)
    {
        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        return new LogoutCommandHandler(unitOfWorkMock.Object, jwtMock.Object, accessorMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenNoCookie()
    {
        var httpContext = new DefaultHttpContext();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var jwtMock = new Mock<IJwtTokenService>();

        var handler = CreateHandler(httpContext, unitOfWorkMock, jwtMock);
        var result = await handler.Handle(new LogoutCommand(), CancellationToken.None);

        result.ToString().Should().Contain("BadRequest");
    }

    [Fact]
    public async Task Handle_ShouldReturnOk_WhenTokenExists()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Cookie = "refresh_token=mytoken";

        var refreshToken = new RefreshToken { Id = Guid.NewGuid() };
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var jwtMock = new Mock<IJwtTokenService>();
        jwtMock.Setup(j => j.Hash("mytoken")).Returns("myhash");
        unitOfWorkMock.Setup(u => u.RefreshTokens.GetByTokenAsync("myhash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);
        unitOfWorkMock.Setup(u => u.RefreshTokens.DeleteAsync(refreshToken.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = CreateHandler(httpContext, unitOfWorkMock, jwtMock);
        var result = await handler.Handle(new LogoutCommand(), CancellationToken.None);

        result.ToString().Should().Contain("Ok");
    }

    [Fact]
    public async Task Handle_ShouldReturnOk_WhenTokenNotFoundInDb()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Cookie = "refresh_token=unknowntoken";

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var jwtMock = new Mock<IJwtTokenService>();
        jwtMock.Setup(j => j.Hash("unknowntoken")).Returns("unknownhash");
        unitOfWorkMock.Setup(u => u.RefreshTokens.GetByTokenAsync("unknownhash", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var handler = CreateHandler(httpContext, unitOfWorkMock, jwtMock);
        var result = await handler.Handle(new LogoutCommand(), CancellationToken.None);

        result.ToString().Should().Contain("Ok");
    }
}
