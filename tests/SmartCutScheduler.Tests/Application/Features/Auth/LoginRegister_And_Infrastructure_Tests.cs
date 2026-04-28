using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using SmartCutScheduler.Application.Common.Behaviors;
using SmartCutScheduler.Application.Common.Interfaces;
using SmartCutScheduler.Application.Features.Appointments.CancelAppointment;
using SmartCutScheduler.Application.Features.Appointments.GetMyAppointments;
using SmartCutScheduler.Application.Features.Auth.Login;
using SmartCutScheduler.Application.Features.Auth.Register;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Enums;
using SmartCutScheduler.Domain.Repositories;
using SmartCutScheduler.Infrastructure.Auth;
using SmartCutScheduler.Infrastructure.Security;
using Xunit;

namespace SmartCutScheduler.Tests.Application.Features;

// ─────────────────────────────── JwtTokenService ────────────────────────────────

public class JwtTokenService_Tests
{
    private static JwtTokenService CreateService() =>
        new JwtTokenService(new JwtOptions
        {
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            SigningKey = "super-secret-key-that-is-long-enough-32chars!",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        });

    [Fact]
    public void GenerateAccessToken_ShouldReturnValidJwt()
    {
        var svc = CreateService();
        var user = new User { Id = Guid.NewGuid(), Name = "Alice", Email = "alice@test.com", Role = UserRole.Customer };

        var token = svc.GenerateAccessToken(user);

        token.Should().NotBeNullOrEmpty();
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        jwt.Issuer.Should().Be("TestIssuer");
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeUserClaims()
    {
        var svc = CreateService();
        var user = new User { Id = Guid.NewGuid(), Name = "Bob", Email = "bob@test.com", Role = UserRole.Barber };

        var token = svc.GenerateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "bob@test.com");
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnTokenAndHash()
    {
        var svc = CreateService();
        var (token, hash, expiresAt) = svc.GenerateRefreshToken();

        token.Should().NotBeNullOrEmpty();
        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe(token);
        expiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void Hash_ShouldReturnHexString()
    {
        var svc = CreateService();
        var hash = svc.Hash("test-value");

        hash.Should().NotBeNullOrEmpty();
        hash.Should().MatchRegex("^[0-9A-F]+$");
    }

    [Fact]
    public void Hash_ShouldBeDeterministic()
    {
        var svc = CreateService();
        var h1 = svc.Hash("same-value");
        var h2 = svc.Hash("same-value");
        h1.Should().Be(h2);
    }
}

// ──────────────────────────── PasswordService ────────────────────────────────────

public class PasswordService_Tests
{
    private static PasswordService CreateService() => new PasswordService(new PasswordHasher<User>());

    [Fact]
    public void Hash_ShouldReturnNonEmptyString()
    {
        var svc = CreateService();
        var user = new User { Id = Guid.NewGuid(), Email = "u@t.com" };
        var hash = svc.Hash(user, "Password1!");
        hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Verify_ShouldReturnTrue_WhenPasswordMatches()
    {
        var svc = CreateService();
        var user = new User { Id = Guid.NewGuid(), Email = "u@t.com" };
        var hash = svc.Hash(user, "Password1!");
        svc.Verify(user, hash, "Password1!").Should().BeTrue();
    }

    [Fact]
    public void Verify_ShouldReturnFalse_WhenPasswordWrong()
    {
        var svc = CreateService();
        var user = new User { Id = Guid.NewGuid(), Email = "u@t.com" };
        var hash = svc.Hash(user, "Password1!");
        svc.Verify(user, hash, "WrongPassword").Should().BeFalse();
    }
}

// ──────────────────────────── LoginCommandHandler ────────────────────────────────

public class LoginCommandHandler_Tests
{
    private static (Mock<IUnitOfWork>, Mock<IPasswordService>, Mock<IJwtTokenService>, DefaultHttpContext) SetupMocks()
    {
        var uow = new Mock<IUnitOfWork>();
        var pwd = new Mock<IPasswordService>();
        var jwt = new Mock<IJwtTokenService>();
        var ctx = new DefaultHttpContext();
        return (uow, pwd, jwt, ctx);
    }

    private static LoginCommandHandler CreateHandler(
        Mock<IUnitOfWork> uow, Mock<IPasswordService> pwd,
        Mock<IJwtTokenService> jwt, HttpContext ctx)
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(ctx);
        return new LoginCommandHandler(uow.Object, pwd.Object, jwt.Object, accessor.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenUserNotFound()
    {
        var (uow, pwd, jwt, ctx) = SetupMocks();
        uow.Setup(u => u.Users.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = CreateHandler(uow, pwd, jwt, ctx);
        var result = await handler.Handle(new LoginCommand("nobody@test.com", "pass"), CancellationToken.None);

        result.ToString().Should().Contain("BadRequest");
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenPasswordWrong()
    {
        var (uow, pwd, jwt, ctx) = SetupMocks();
        var user = new User { Id = Guid.NewGuid(), Email = "u@t.com", PasswordHash = "hash", Name = "U" };
        uow.Setup(u => u.Users.GetByEmailAsync("u@t.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        pwd.Setup(p => p.Verify(user, "hash", "wrongpass")).Returns(false);

        var handler = CreateHandler(uow, pwd, jwt, ctx);
        var result = await handler.Handle(new LoginCommand("u@t.com", "wrongpass"), CancellationToken.None);

        result.ToString().Should().Contain("BadRequest");
    }

    [Fact]
    public async Task Handle_ShouldReturnOk_WhenCredentialsValid()
    {
        var (uow, pwd, jwt, ctx) = SetupMocks();
        var expires = DateTime.UtcNow.AddDays(7);
        var user = new User { Id = Guid.NewGuid(), Email = "u@t.com", PasswordHash = "hash", Name = "U", Role = UserRole.Customer };

        uow.Setup(u => u.Users.GetByEmailAsync("u@t.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        pwd.Setup(p => p.Verify(user, "hash", "pass")).Returns(true);
        jwt.Setup(j => j.GenerateRefreshToken()).Returns(("rt", "rthash", expires));
        jwt.Setup(j => j.GenerateAccessToken(user)).Returns("accesstoken");
        uow.Setup(u => u.RefreshTokens.DeleteByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        uow.Setup(u => u.RefreshTokens.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = CreateHandler(uow, pwd, jwt, ctx);
        var result = await handler.Handle(new LoginCommand("u@t.com", "pass"), CancellationToken.None);

        result.ToString().Should().Contain("Ok");
    }
}

// ─────────────────────────── RegisterCommandHandler ──────────────────────────────

public class RegisterCommandHandler_Tests
{
    [Fact]
    public async Task Handle_ShouldReturnOk_WhenRegistrationSuccessful()
    {
        var expires = DateTime.UtcNow.AddDays(7);
        var uow = new Mock<IUnitOfWork>();
        var pwd = new Mock<IPasswordService>();
        var jwt = new Mock<IJwtTokenService>();
        var ctx = new DefaultHttpContext();

        pwd.Setup(p => p.Hash(It.IsAny<User>(), It.IsAny<string>())).Returns("hashed");
        jwt.Setup(j => j.GenerateRefreshToken()).Returns(("rt", "rthash", expires));
        jwt.Setup(j => j.GenerateAccessToken(It.IsAny<User>())).Returns("accesstoken");
        uow.Setup(u => u.Users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        uow.Setup(u => u.RefreshTokens.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(ctx);

        var handler = new RegisterCommandHandler(uow.Object, pwd.Object, jwt.Object, accessor.Object);
        var result = await handler.Handle(
            new RegisterCommand("Alice", "alice@test.com", "Password1!", "0700"),
            CancellationToken.None);

        result.ToString().Should().Contain("Ok");
        uow.Verify(u => u.Users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

// ─────────────────────────── CancelAppointmentCommandHandler ─────────────────────

public class CancelAppointmentCommandHandler_Tests
{
    private static (Mock<IAppointmentRepository>, Mock<IUnitOfWork>, Mock<IHttpContextAccessor>) SetupMocks(
        bool authenticated = true, string? userId = null)
    {
        var repo = new Mock<IAppointmentRepository>();
        var uow = new Mock<IUnitOfWork>();
        var accessor = new Mock<IHttpContextAccessor>();
        var ctx = new DefaultHttpContext();

        if (authenticated && userId != null)
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        }

        accessor.Setup(a => a.HttpContext).Returns(ctx);
        return (repo, uow, accessor);
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        var (repo, uow, accessor) = SetupMocks(false);
        var handler = new CancelAppointmentCommandHandler(repo.Object, uow.Object, accessor.Object);
        var result = await handler.Handle(new CancelAppointmentCommand(Guid.NewGuid()), CancellationToken.None);
        result.ToString().Should().Contain("Unauthorized");
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenAppointmentMissing()
    {
        var userId = Guid.NewGuid().ToString();
        var (repo, uow, accessor) = SetupMocks(true, userId);
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appointment?)null);

        var handler = new CancelAppointmentCommandHandler(repo.Object, uow.Object, accessor.Object);
        var result = await handler.Handle(new CancelAppointmentCommand(Guid.NewGuid()), CancellationToken.None);
        result.ToString().Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenAlreadyCancelled()
    {
        var userId = Guid.NewGuid();
        var (repo, uow, accessor) = SetupMocks(true, userId.ToString());
        var appt = new Appointment { Id = Guid.NewGuid(), UserId = userId, Status = AppointmentStatus.Cancelled };
        repo.Setup(r => r.GetByIdAsync(appt.Id, It.IsAny<CancellationToken>())).ReturnsAsync(appt);

        var handler = new CancelAppointmentCommandHandler(repo.Object, uow.Object, accessor.Object);
        var result = await handler.Handle(new CancelAppointmentCommand(appt.Id), CancellationToken.None);
        result.ToString().Should().Contain("BadRequest");
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenCompleted()
    {
        var userId = Guid.NewGuid();
        var (repo, uow, accessor) = SetupMocks(true, userId.ToString());
        var appt = new Appointment { Id = Guid.NewGuid(), UserId = userId, Status = AppointmentStatus.Completed };
        repo.Setup(r => r.GetByIdAsync(appt.Id, It.IsAny<CancellationToken>())).ReturnsAsync(appt);

        var handler = new CancelAppointmentCommandHandler(repo.Object, uow.Object, accessor.Object);
        var result = await handler.Handle(new CancelAppointmentCommand(appt.Id), CancellationToken.None);
        result.ToString().Should().Contain("BadRequest");
    }

    [Fact]
    public async Task Handle_ShouldReturnOk_WhenCancellationSuccessful()
    {
        var userId = Guid.NewGuid();
        var (repo, uow, accessor) = SetupMocks(true, userId.ToString());
        var appt = new Appointment { Id = Guid.NewGuid(), UserId = userId, Status = AppointmentStatus.Pending };
        repo.Setup(r => r.GetByIdAsync(appt.Id, It.IsAny<CancellationToken>())).ReturnsAsync(appt);
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new CancelAppointmentCommandHandler(repo.Object, uow.Object, accessor.Object);
        var result = await handler.Handle(new CancelAppointmentCommand(appt.Id), CancellationToken.None);
        result.ToString().Should().Contain("Ok");
        appt.Status.Should().Be(AppointmentStatus.Cancelled);
    }
}

// ───────────────────────── GetBarberAppointmentsQueryHandler ─────────────────────

public class GetBarberAppointmentsQueryHandler_Tests
{
    private static GetBarberAppointmentsQueryHandler CreateHandler(
        Mock<IAppointmentRepository> repo, bool authenticated = true, string? userId = null)
    {
        var accessor = new Mock<IHttpContextAccessor>();
        var ctx = new DefaultHttpContext();

        if (authenticated && userId != null)
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        }

        accessor.Setup(a => a.HttpContext).Returns(ctx);
        return new GetBarberAppointmentsQueryHandler(repo.Object, accessor.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        var repo = new Mock<IAppointmentRepository>();
        var handler = CreateHandler(repo, false);
        var result = await handler.Handle(new GetBarberAppointmentsQuery(), CancellationToken.None);
        result.ToString().Should().Contain("Unauthorized");
    }

    [Fact]
    public async Task Handle_ShouldReturnOk_WithAppointments()
    {
        var barberId = Guid.NewGuid();
        var repo = new Mock<IAppointmentRepository>();
        repo.Setup(r => r.GetByBarberIdAsync(barberId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment>
            {
                new Appointment
                {
                    Id = Guid.NewGuid(), BarberId = barberId,
                    AppointmentDate = DateTime.Today, StartTime = new TimeSpan(10, 0, 0),
                    EndTime = new TimeSpan(10, 30, 0), Status = AppointmentStatus.Confirmed
                }
            });

        var handler = CreateHandler(repo, true, barberId.ToString());
        var result = await handler.Handle(new GetBarberAppointmentsQuery(), CancellationToken.None);
        result.ToString().Should().Contain("Ok");
    }

    [Fact]
    public async Task Handle_ShouldReturnOk_WithEmptyList()
    {
        var barberId = Guid.NewGuid();
        var repo = new Mock<IAppointmentRepository>();
        repo.Setup(r => r.GetByBarberIdAsync(barberId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Appointment>());

        var handler = CreateHandler(repo, true, barberId.ToString());
        var result = await handler.Handle(new GetBarberAppointmentsQuery(), CancellationToken.None);
        result.ToString().Should().Contain("Ok");
    }
}

// ─────────────────────────── ValidationBehavior ──────────────────────────────────

public record ValidationTestRequest(string Value) : IRequest<string>;

public class ValidationTestRequestValidator : AbstractValidator<ValidationTestRequest>
{
    public ValidationTestRequestValidator(bool shouldFail)
    {
        if (shouldFail)
            RuleFor(x => x.Value).Must(_ => false).WithMessage("Always fails");
    }
}

public class ValidationBehavior_Tests
{
    [Fact]
    public async Task Handle_ShouldCallNext_WhenNoValidators()
    {
        var behavior = new ValidationBehavior<ValidationTestRequest, string>(Enumerable.Empty<IValidator<ValidationTestRequest>>());
        var next = new RequestHandlerDelegate<string>(ct => Task.FromResult("result"));

        var result = await behavior.Handle(new ValidationTestRequest("x"), next, CancellationToken.None);
        result.Should().Be("result");
    }

    [Fact]
    public async Task Handle_ShouldCallNext_WhenValidationPasses()
    {
        var validator = new ValidationTestRequestValidator(shouldFail: false);
        var behavior = new ValidationBehavior<ValidationTestRequest, string>(new[] { validator });
        var next = new RequestHandlerDelegate<string>(ct => Task.FromResult("ok"));

        var result = await behavior.Handle(new ValidationTestRequest("x"), next, CancellationToken.None);
        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenValidationFails()
    {
        var validator = new ValidationTestRequestValidator(shouldFail: true);
        var behavior = new ValidationBehavior<ValidationTestRequest, string>(new[] { validator });
        var next = new RequestHandlerDelegate<string>(ct => Task.FromResult("never"));

        await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(new ValidationTestRequest("x"), next, CancellationToken.None));
    }
}
