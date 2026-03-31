using Xunit;
using Moq;
using FluentAssertions;
using SmartCutScheduler.Infrastructure.Repositories;
using SmartCutScheduler.Domain.Repositories;
using SmartCutScheduler.Domain.Entities;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using SmartCutScheduler.Infrastructure.Persistence;

namespace SmartCutScheduler.Tests.Infrastructure.Repositories;

public class UnitOfWorkIntegrationTests
{
    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static UnitOfWork CreateUnitOfWork(AppDbContext context)
    {
        var userRepo = new Mock<IUserRepository>().Object;
        var barberRepo = new Mock<IBarberRepository>().Object;
        var serviceRepo = new Mock<IServiceRepository>().Object;
        var appointmentRepo = new Mock<IAppointmentRepository>().Object;
        var refreshTokenRepo = new Mock<IRefreshTokenRepository>().Object;
        return new UnitOfWork(context, userRepo, barberRepo, serviceRepo, appointmentRepo, refreshTokenRepo);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldPersistEntity_ToInMemoryDb()
    {
        using var context = CreateInMemoryContext();
        var uow = CreateUnitOfWork(context);
        var service = new Service
        {
            Id = Guid.NewGuid(),
            Name = "Tuns",
            DurationMinutes = 30,
            BasePrice = 40m,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.Services.Add(service);

        var affected = await uow.SaveChangesAsync(CancellationToken.None);

        affected.Should().Be(1);
        context.Services.Should().ContainSingle(s => s.Id == service.Id);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldReturnZero_WhenNoChanges()
    {
        using var context = CreateInMemoryContext();
        var uow = CreateUnitOfWork(context);

        var affected = await uow.SaveChangesAsync(CancellationToken.None);

        affected.Should().Be(0);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldPersistMultipleEntities()
    {
        using var context = CreateInMemoryContext();
        var uow = CreateUnitOfWork(context);

        for (int i = 0; i < 3; i++)
        {
            context.Services.Add(new Service
            {
                Id = Guid.NewGuid(),
                Name = $"Service {i}",
                DurationMinutes = 30,
                BasePrice = 10m * (i + 1),
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }

        var affected = await uow.SaveChangesAsync(CancellationToken.None);

        affected.Should().Be(3);
        context.Services.Should().HaveCount(3);
    }

    [Fact]
    public void UnitOfWork_ShouldExpose_AllRepositoryProperties()
    {
        using var context = CreateInMemoryContext();
        var uow = CreateUnitOfWork(context);

        uow.Users.Should().NotBeNull();
        uow.Barbers.Should().NotBeNull();
        uow.Services.Should().NotBeNull();
        uow.Appointments.Should().NotBeNull();
        uow.RefreshTokens.Should().NotBeNull();
    }
}
