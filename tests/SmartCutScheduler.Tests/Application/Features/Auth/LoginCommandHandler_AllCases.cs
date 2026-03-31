using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using SmartCutScheduler.Application.Features.Auth.Login;
using SmartCutScheduler.Domain.Repositories;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System;

public class LoginCommandHandler_AllCases
{
    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenUserNotFound()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var passwordServiceMock = new Mock<IPasswordService>();
        var jwtTokenServiceMock = new Mock<IJwtTokenService>();
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        unitOfWorkMock.Setup(u => u.Users.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User)null);
        var handler = new LoginCommandHandler(unitOfWorkMock.Object, passwordServiceMock.Object, jwtTokenServiceMock.Object, httpContextAccessorMock.Object);
        var command = new LoginCommand("test@test.com", "pass");
        var result = await handler.Handle(command, CancellationToken.None);
        result.ToString().Should().Contain("BadRequest");
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenPasswordInvalid()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var passwordServiceMock = new Mock<IPasswordService>();
        var jwtTokenServiceMock = new Mock<IJwtTokenService>();
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        unitOfWorkMock.Setup(u => u.Users.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new User());
        passwordServiceMock.Setup(p => p.Verify(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        var handler = new LoginCommandHandler(unitOfWorkMock.Object, passwordServiceMock.Object, jwtTokenServiceMock.Object, httpContextAccessorMock.Object);
        var command = new LoginCommand("test@test.com", "wrong");
        var result = await handler.Handle(command, CancellationToken.None);
        result.ToString().Should().Contain("BadRequest");
    }

    [Fact]
    public async Task Handle_ShouldReturnOk_WhenLoginIsValid()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var passwordServiceMock = new Mock<IPasswordService>();
        var jwtTokenServiceMock = new Mock<IJwtTokenService>();
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        var user = new User { PasswordHash = "hash" };
        unitOfWorkMock.Setup(u => u.Users.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        passwordServiceMock.Setup(p => p.Verify(user, "hash", It.IsAny<string>())).Returns(true);
        jwtTokenServiceMock.Setup(j => j.GenerateRefreshToken()).Returns(("rt", "rthash", DateTime.UtcNow.AddDays(1)));
        jwtTokenServiceMock.Setup(j => j.GenerateAccessToken(user)).Returns("access");
        unitOfWorkMock.Setup(u => u.RefreshTokens.DeleteByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWorkMock.Setup(u => u.RefreshTokens.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var httpContext = new DefaultHttpContext();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        var handler = new LoginCommandHandler(unitOfWorkMock.Object, passwordServiceMock.Object, jwtTokenServiceMock.Object, httpContextAccessorMock.Object);
        var command = new LoginCommand("test@test.com", "pass");
        var result = await handler.Handle(command, CancellationToken.None);
        result.ToString().Should().Contain("Ok");
    }
}
