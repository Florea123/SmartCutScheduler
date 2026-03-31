using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using SmartCutScheduler.Application.Features.Auth.Register;
using SmartCutScheduler.Domain.Repositories;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System;

public class RegisterCommandHandler_AllCases
{
    [Fact]
    public async Task Handle_ShouldCreateUser_WhenInputIsValid()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var passwordServiceMock = new Mock<IPasswordService>();
        var jwtTokenServiceMock = new Mock<IJwtTokenService>();
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        passwordServiceMock.Setup(p => p.Hash(It.IsAny<User>(), It.IsAny<string>())).Returns("hash");
        jwtTokenServiceMock.Setup(j => j.GenerateRefreshToken()).Returns(("rt", "rthash", DateTime.UtcNow.AddDays(1)));
        jwtTokenServiceMock.Setup(j => j.GenerateAccessToken(It.IsAny<User>())).Returns("access");
        unitOfWorkMock.Setup(u => u.Users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWorkMock.Setup(u => u.RefreshTokens.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var handler = new RegisterCommandHandler(unitOfWorkMock.Object, passwordServiceMock.Object, jwtTokenServiceMock.Object, httpContextAccessorMock.Object);
        var command = new RegisterCommand("Test", "test@test.com", "pass", "123");
        var result = await handler.Handle(command, CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ShouldSetPasswordHash()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var passwordServiceMock = new Mock<IPasswordService>();
        var jwtTokenServiceMock = new Mock<IJwtTokenService>();
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        passwordServiceMock.Setup(p => p.Hash(It.IsAny<User>(), It.IsAny<string>())).Returns("hash");
        jwtTokenServiceMock.Setup(j => j.GenerateRefreshToken()).Returns(("rt", "rthash", DateTime.UtcNow.AddDays(1)));
        jwtTokenServiceMock.Setup(j => j.GenerateAccessToken(It.IsAny<User>())).Returns("access");
        unitOfWorkMock.Setup(u => u.Users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWorkMock.Setup(u => u.RefreshTokens.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var handler = new RegisterCommandHandler(unitOfWorkMock.Object, passwordServiceMock.Object, jwtTokenServiceMock.Object, httpContextAccessorMock.Object);
        var command = new RegisterCommand("Test", "test@test.com", "pass", "123");
        await handler.Handle(command, CancellationToken.None);
        passwordServiceMock.Verify(p => p.Hash(It.IsAny<User>(), "pass"), Times.Once);
    }

    // Alte teste: input invalid, duplicate email, etc.
}
