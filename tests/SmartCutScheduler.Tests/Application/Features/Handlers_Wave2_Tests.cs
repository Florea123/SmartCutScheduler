using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using SmartCutScheduler.Application.Features.BarberServices.AddService;
using SmartCutScheduler.Application.Features.BarberServices.GetMyServices;
using SmartCutScheduler.Application.Features.BarberServices.RemoveService;
using SmartCutScheduler.Application.Features.Barbers.CreateBarber;
using SmartCutScheduler.Application.Features.Barbers.DeleteBarber;
using SmartCutScheduler.Application.Features.Barbers.GetAllBarbers;
using SmartCutScheduler.Application.Features.Reviews.GetBarberReviews;
using SmartCutScheduler.Application.Features.Reviews.UpsertReview;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Enums;
using SmartCutScheduler.Domain.Repositories;
using Xunit;

namespace SmartCutScheduler.Tests.Application.Features;

// ─────────────────────────── GetAllBarbersQueryHandler ──────────────────────────

public class GetAllBarbersQueryHandler_Tests
{
    private static Mock<IBarberRepository> BarberRepo(IEnumerable<Barber> barbers)
    {
        var mock = new Mock<IBarberRepository>();
        mock.Setup(r => r.GetAllAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(barbers);
        return mock;
    }

    [Fact]
    public async Task Handle_ShouldReturnOkWithBarberList()
    {
        var barber = new Barber
        {
            Id = Guid.NewGuid(), Name = "B", Email = "b@t.com",
            IsActive = true, BarberServices = new List<BarberService>(), Reviews = new List<Review>()
        };
        var handler = new GetAllBarbersQueryHandler(BarberRepo(new[] { barber }).Object);
        var result = await handler.Handle(new GetAllBarbersQuery(), CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnOkWithEmptyList()
    {
        var handler = new GetAllBarbersQueryHandler(BarberRepo(Array.Empty<Barber>()).Object);
        var result = await handler.Handle(new GetAllBarbersQuery(), CancellationToken.None);
        result.Should().NotBeNull();
    }
}

// ──────────────────────────── DeleteBarberCommandHandler ────────────────────────

public class DeleteBarberCommandHandler_Tests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenBarberMissing()
    {
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Barbers.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Barber?)null);

        var handler = new DeleteBarberCommandHandler(uow.Object);
        var result = await handler.Handle(new DeleteBarberCommand(Guid.NewGuid()), CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnOk_WhenBarberExists()
    {
        var barber = new Barber { Id = Guid.NewGuid(), Name = "B", Email = "b@t.com" };
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Barbers.GetByIdAsync(barber.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(barber);
        uow.Setup(u => u.Barbers.DeleteAsync(barber.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new DeleteBarberCommandHandler(uow.Object);
        var result = await handler.Handle(new DeleteBarberCommand(barber.Id), CancellationToken.None);
        result.Should().NotBeNull();
    }
}

// ──────────────────────────── CreateBarberCommandHandler ────────────────────────

public class CreateBarberCommandHandler_Tests
{
    [Fact]
    public async Task Handle_ShouldThrow_WhenEmailAlreadyExists()
    {
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Users.GetByEmailAsync("existing@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = Guid.NewGuid(), Name = "Existing", Email = "existing@test.com", PasswordHash = "h" });

        var hasher = new PasswordHasher<User>();
        var handler = new CreateBarberCommandHandler(uow.Object, hasher);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CreateBarberCommand("Name", "existing@test.com", "0700", "pass", "desc", null), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldCreateBarberAndReturnGuid()
    {
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Users.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        uow.Setup(u => u.Users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        uow.Setup(u => u.Barbers.AddAsync(It.IsAny<Barber>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var hasher = new PasswordHasher<User>();
        var handler = new CreateBarberCommandHandler(uow.Object, hasher);

        var id = await handler.Handle(new CreateBarberCommand("New", "new@test.com", "0700", "pass", "desc", null), CancellationToken.None);
        id.Should().NotBeEmpty();
    }
}

// ──────────────────────────── AddServiceCommandHandler ──────────────────────────

public class AddServiceCommandHandler_Tests
{
    [Fact]
    public async Task Handle_ShouldThrow_WhenBarberNotFound()
    {
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Barbers.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Barber?)null);

        var handler = new AddServiceCommandHandler(uow.Object);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new AddServiceCommand(Guid.NewGuid(), Guid.NewGuid(), null), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenServiceNotFound()
    {
        var barber = new Barber { Id = Guid.NewGuid(), Name = "B", Email = "b@t.com", BarberServices = new List<BarberService>() };
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Barbers.GetByIdAsync(barber.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(barber);
        uow.Setup(u => u.Services.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Service?)null);

        var handler = new AddServiceCommandHandler(uow.Object);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new AddServiceCommand(barber.Id, Guid.NewGuid(), null), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenServiceAlreadyAdded()
    {
        var serviceId = Guid.NewGuid();
        var barber = new Barber
        {
            Id = Guid.NewGuid(), Name = "B", Email = "b@t.com",
            BarberServices = new List<BarberService> { new BarberService { ServiceId = serviceId } }
        };
        var service = new Service { Id = serviceId, Name = "S", DurationMinutes = 30, BasePrice = 50, IsActive = true };

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Barbers.GetByIdAsync(barber.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(barber);
        uow.Setup(u => u.Services.GetByIdAsync(serviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        var handler = new AddServiceCommandHandler(uow.Object);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new AddServiceCommand(barber.Id, serviceId, null), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldAddService_WhenValid()
    {
        var serviceId = Guid.NewGuid();
        var barber = new Barber { Id = Guid.NewGuid(), Name = "B", Email = "b@t.com", BarberServices = new List<BarberService>() };
        var service = new Service { Id = serviceId, Name = "S", DurationMinutes = 30, BasePrice = 50, IsActive = true };

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Barbers.GetByIdAsync(barber.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(barber);
        uow.Setup(u => u.Services.GetByIdAsync(serviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new AddServiceCommandHandler(uow.Object);
        var result = await handler.Handle(new AddServiceCommand(barber.Id, serviceId, 100m), CancellationToken.None);
        result.Should().Be(serviceId);
    }
}

// ─────────────────────────── RemoveServiceCommandHandler ────────────────────────

public class RemoveServiceCommandHandler_Tests
{
    [Fact]
    public async Task Handle_ShouldThrow_WhenBarberNotFound()
    {
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Barbers.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Barber?)null);

        var handler = new RemoveServiceCommandHandler(uow.Object);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new RemoveServiceCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenServiceNotOnBarber()
    {
        var barber = new Barber { Id = Guid.NewGuid(), Name = "B", Email = "b@t.com", BarberServices = new List<BarberService>() };
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Barbers.GetByIdAsync(barber.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(barber);

        var handler = new RemoveServiceCommandHandler(uow.Object);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new RemoveServiceCommand(barber.Id, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldReturnTrue_WhenServiceRemoved()
    {
        var serviceId = Guid.NewGuid();
        var bs = new BarberService { ServiceId = serviceId };
        var barber = new Barber { Id = Guid.NewGuid(), Name = "B", Email = "b@t.com", BarberServices = new List<BarberService> { bs } };

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Barbers.GetByIdAsync(barber.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(barber);
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new RemoveServiceCommandHandler(uow.Object);
        var result = await handler.Handle(new RemoveServiceCommand(barber.Id, serviceId), CancellationToken.None);
        result.Should().BeTrue();
    }
}

// ─────────────────────────── GetMyServicesQueryHandler ──────────────────────────

public class GetMyServicesQueryHandler_Tests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenBarberMissing()
    {
        var repo = new Mock<IBarberRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Barber?)null);

        var handler = new GetMyServicesQueryHandler(repo.Object);
        var result = await handler.Handle(new GetMyServicesQuery(Guid.NewGuid()), CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnOk_WithServiceList()
    {
        var serviceId = Guid.NewGuid();
        var service = new Service { Id = serviceId, Name = "Cut", DurationMinutes = 30, BasePrice = 50 };
        var barber = new Barber
        {
            Id = Guid.NewGuid(), Name = "B", Email = "b@t.com",
            BarberServices = new List<BarberService>
            {
                new BarberService { ServiceId = serviceId, Service = service, CustomPrice = null }
            }
        };

        var repo = new Mock<IBarberRepository>();
        repo.Setup(r => r.GetByIdAsync(barber.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(barber);

        var handler = new GetMyServicesQueryHandler(repo.Object);
        var result = await handler.Handle(new GetMyServicesQuery(barber.Id), CancellationToken.None);
        result.Should().NotBeNull();
    }
}

// ──────────────────────────── GetBarberReviewsQueryHandler ──────────────────────

public class GetBarberReviewsQueryHandler_Tests
{
    [Fact]
    public async Task Handle_ShouldReturnOkWithReviews()
    {
        var barberId = Guid.NewGuid();
        var review = new Review
        {
            Id = Guid.NewGuid(), BarberId = barberId, UserId = Guid.NewGuid(),
            Rating = 5, Comment = "Great!", CreatedAt = DateTime.UtcNow,
            User = new User { Id = Guid.NewGuid(), Name = "Alice", Email = "a@t.com", PasswordHash = "h" }
        };

        var repo = new Mock<IReviewRepository>();
        repo.Setup(r => r.GetReviewsForBarberAsync(barberId))
            .ReturnsAsync(new[] { review });

        var handler = new GetBarberReviewsQueryHandler(repo.Object);
        var result = await handler.Handle(new GetBarberReviewsQuery(barberId), CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnOkWithEmptyList()
    {
        var repo = new Mock<IReviewRepository>();
        repo.Setup(r => r.GetReviewsForBarberAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Array.Empty<Review>());

        var handler = new GetBarberReviewsQueryHandler(repo.Object);
        var result = await handler.Handle(new GetBarberReviewsQuery(Guid.NewGuid()), CancellationToken.None);
        result.Should().NotBeNull();
    }
}

// ────────────────────────── UpsertReviewCommandValidator ────────────────────────

public class UpsertReviewCommandValidator_Tests
{
    private readonly UpsertReviewCommandValidator _validator = new();

    [Fact]
    public void Valid_ShouldPass()
    {
        var cmd = new UpsertReviewCommand(Guid.NewGuid(), 4, "Good");
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyBarberId_ShouldFail()
    {
        var cmd = new UpsertReviewCommand(Guid.Empty, 4, null);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.BarberId);
    }

    [Fact]
    public void Rating_OutOfRange_ShouldFail()
    {
        var cmd = new UpsertReviewCommand(Guid.NewGuid(), 6, null);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Rating);
    }

    [Fact]
    public void Comment_TooLong_ShouldFail()
    {
        var cmd = new UpsertReviewCommand(Guid.NewGuid(), 3, new string('x', 1001));
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Comment);
    }
}

// ──────────────────────── UpsertReviewCommandHandler ────────────────────────────

public class UpsertReviewCommandHandler_Tests
{
    private static (Mock<IReviewRepository>, Mock<IHttpContextAccessor>, DefaultHttpContext) Setup(bool authenticated, string? userId = null)
    {
        var repo = new Mock<IReviewRepository>();
        var accessor = new Mock<IHttpContextAccessor>();
        var httpCtx = new DefaultHttpContext();

        if (authenticated && userId != null)
        {
            var claims = new[] { new Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId) };
            httpCtx.User = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(claims, "Bearer"));
        }

        accessor.Setup(a => a.HttpContext).Returns(httpCtx);
        return (repo, accessor, httpCtx);
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        var (repo, accessor, _) = Setup(false);
        var handler = new UpsertReviewCommandHandler(repo.Object, accessor.Object);
        var result = await handler.Handle(new UpsertReviewCommand(Guid.NewGuid(), 5, null), CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnOk_WhenAuthenticated()
    {
        var userId = Guid.NewGuid().ToString();
        var (repo, accessor, _) = Setup(true, userId);
        repo.Setup(r => r.AddOrUpdateReviewAsync(It.IsAny<Review>()))
            .Returns(Task.CompletedTask);

        var handler = new UpsertReviewCommandHandler(repo.Object, accessor.Object);
        var result = await handler.Handle(new UpsertReviewCommand(Guid.NewGuid(), 5, "Nice"), CancellationToken.None);
        result.Should().NotBeNull();
    }
}
