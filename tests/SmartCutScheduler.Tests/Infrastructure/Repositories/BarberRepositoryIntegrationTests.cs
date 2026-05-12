using Microsoft.EntityFrameworkCore;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Infrastructure.Persistence;
using SmartCutScheduler.Infrastructure.Repositories;
using Testcontainers.PostgreSql;

namespace SmartCutScheduler.Tests.Infrastructure.Repositories;

/// <summary>
/// Integration tests using a real PostgreSQL instance via Testcontainers.
/// Each test class spins up a throwaway Docker container and tears it down after.
/// </summary>
public sealed class BarberRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("smartcut_test")
        .WithUsername("test_user")
        .WithPassword("test_pass")
        .Build();

    private AppDbContext _db = null!;
    private BarberRepository _repository = null!;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        _db = new AppDbContext(options);
        await _db.Database.MigrateAsync();

        _repository = new BarberRepository(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoBarbersExist()
    {
        var result = await _repository.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AddAsync_PersistsBarber_AndCanBeRetrieved()
    {
        var barber = new Barber
        {
            Id = Guid.NewGuid(),
            Name = "Test Barber",
            Email = "test@barber.com",
            PhoneNumber = "0700000000",
            Description = "Integration test barber",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.Barbers.Add(barber);
        await _db.SaveChangesAsync();

        var retrieved = await _repository.GetByIdAsync(barber.Id);

        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Test Barber");
        retrieved.Email.Should().Be("test@barber.com");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllActiveBarbers()
    {
        var active = new Barber
        {
            Id = Guid.NewGuid(),
            Name = "Active Barber",
            Email = "active@barber.com",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        var inactive = new Barber
        {
            Id = Guid.NewGuid(),
            Name = "Inactive Barber",
            Email = "inactive@barber.com",
            IsActive = false,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.Barbers.AddRange(active, inactive);
        await _db.SaveChangesAsync();

        var result = await _repository.GetAllAsync();

        result.Should().Contain(b => b.Name == "Active Barber");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenBarberDoesNotExist()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_RemovesBarber_FromDatabase()
    {
        var barber = new Barber
        {
            Id = Guid.NewGuid(),
            Name = "To Delete",
            Email = "delete@barber.com",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.Barbers.Add(barber);
        await _db.SaveChangesAsync();

        _db.Barbers.Remove(barber);
        await _db.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(barber.Id);
        result.Should().BeNull();
    }
}
