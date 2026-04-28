using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Enums;
using SmartCutScheduler.Infrastructure.Persistence;
using SmartCutScheduler.Infrastructure.Repositories;
using Xunit;

namespace SmartCutScheduler.Tests.Infrastructure.Repositories;

public class Repository_Combined_Tests
{
    private static AppDbContext CreateContext() =>
        new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    // ─────────────────────────────── UserRepository ───────────────────────────────────

    [Fact]
    public async Task UserRepo_AddAndGetById_ShouldWork()
    {
        using var ctx = CreateContext();
        var repo = new UserRepository(ctx);
        var user = new User { Id = Guid.NewGuid(), Name = "Alice", Email = "alice@test.com", PasswordHash = "hash" };
        await repo.AddAsync(user);
        await ctx.SaveChangesAsync();

        var found = await repo.GetByIdAsync(user.Id);
        found.Should().NotBeNull();
        found!.Name.Should().Be("Alice");
    }

    [Fact]
    public async Task UserRepo_GetByEmail_ShouldReturnUser()
    {
        using var ctx = CreateContext();
        var repo = new UserRepository(ctx);
        var user = new User { Id = Guid.NewGuid(), Name = "Bob", Email = "bob@test.com", PasswordHash = "hash" };
        await repo.AddAsync(user);
        await ctx.SaveChangesAsync();

        var found = await repo.GetByEmailAsync("BOB@TEST.COM");
        found.Should().NotBeNull();
    }

    [Fact]
    public async Task UserRepo_GetByEmail_ShouldReturnNull_WhenNotFound()
    {
        using var ctx = CreateContext();
        var repo = new UserRepository(ctx);
        var found = await repo.GetByEmailAsync("nobody@test.com");
        found.Should().BeNull();
    }

    [Fact]
    public async Task UserRepo_GetAll_ShouldReturnAll()
    {
        using var ctx = CreateContext();
        var repo = new UserRepository(ctx);
        await repo.AddAsync(new User { Id = Guid.NewGuid(), Name = "A", Email = "a@test.com", PasswordHash = "hash" });
        await repo.AddAsync(new User { Id = Guid.NewGuid(), Name = "B", Email = "b@test.com", PasswordHash = "hash" });
        await ctx.SaveChangesAsync();

        var all = await repo.GetAllAsync();
        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task UserRepo_Delete_ShouldRemoveUser()
    {
        using var ctx = CreateContext();
        var repo = new UserRepository(ctx);
        var user = new User { Id = Guid.NewGuid(), Name = "Delete Me", Email = "del@test.com", PasswordHash = "hash" };
        await repo.AddAsync(user);
        await ctx.SaveChangesAsync();

        await repo.DeleteAsync(user.Id);
        await ctx.SaveChangesAsync();

        var found = await repo.GetByIdAsync(user.Id);
        found.Should().BeNull();
    }

    [Fact]
    public async Task UserRepo_Update_ShouldPersistChanges()
    {
        using var ctx = CreateContext();
        var repo = new UserRepository(ctx);
        var user = new User { Id = Guid.NewGuid(), Name = "Old Name", Email = "update@test.com", PasswordHash = "hash" };
        await repo.AddAsync(user);
        await ctx.SaveChangesAsync();

        user.Name = "New Name";
        await repo.UpdateAsync(user);
        await ctx.SaveChangesAsync();

        var found = await repo.GetByIdAsync(user.Id);
        found!.Name.Should().Be("New Name");
    }

    // ───────────────────────────── ServiceRepository ──────────────────────────────────

    [Fact]
    public async Task ServiceRepo_AddAndGetById_ShouldWork()
    {
        using var ctx = CreateContext();
        var repo = new ServiceRepository(ctx);
        var service = new Service { Id = Guid.NewGuid(), Name = "Cut", DurationMinutes = 30, BasePrice = 50, IsActive = true };
        await repo.AddAsync(service);
        await ctx.SaveChangesAsync();

        var found = await repo.GetByIdAsync(service.Id);
        found.Should().NotBeNull();
    }

    [Fact]
    public async Task ServiceRepo_GetAll_ShouldExcludeInactive()
    {
        using var ctx = CreateContext();
        var repo = new ServiceRepository(ctx);
        await repo.AddAsync(new Service { Id = Guid.NewGuid(), Name = "Active", DurationMinutes = 30, BasePrice = 50, IsActive = true });
        await repo.AddAsync(new Service { Id = Guid.NewGuid(), Name = "Inactive", DurationMinutes = 30, BasePrice = 50, IsActive = false });
        await ctx.SaveChangesAsync();

        var active = await repo.GetAllAsync(includeInactive: false);
        active.Should().HaveCount(1);
    }

    [Fact]
    public async Task ServiceRepo_GetAll_ShouldIncludeInactive_WhenFlagSet()
    {
        using var ctx = CreateContext();
        var repo = new ServiceRepository(ctx);
        await repo.AddAsync(new Service { Id = Guid.NewGuid(), Name = "A", DurationMinutes = 30, BasePrice = 50, IsActive = true });
        await repo.AddAsync(new Service { Id = Guid.NewGuid(), Name = "B", DurationMinutes = 30, BasePrice = 50, IsActive = false });
        await ctx.SaveChangesAsync();

        var all = await repo.GetAllAsync(includeInactive: true);
        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task ServiceRepo_Delete_ShouldRemoveService()
    {
        using var ctx = CreateContext();
        var repo = new ServiceRepository(ctx);
        var service = new Service { Id = Guid.NewGuid(), Name = "Trim", DurationMinutes = 15, BasePrice = 25, IsActive = true };
        await repo.AddAsync(service);
        await ctx.SaveChangesAsync();

        await repo.DeleteAsync(service.Id);
        await ctx.SaveChangesAsync();

        var found = await repo.GetByIdAsync(service.Id);
        found.Should().BeNull();
    }

    // ───────────────────────────── AppointmentRepository ─────────────────────────────

    [Fact]
    public async Task AppointmentRepo_AddAndGetById_ShouldWork()
    {
        using var ctx = CreateContext();
        var barberId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var date = new DateTime(2024, 5, 1);

        ctx.Users.Add(new User { Id = userId, Name = "U", Email = "u@t.com", PasswordHash = "hash" });
        ctx.Barbers.Add(new Barber { Id = barberId, Name = "B", Email = "b@t.com" });
        ctx.Services.Add(new Service { Id = serviceId, Name = "S", DurationMinutes = 30, BasePrice = 50 });
        await ctx.SaveChangesAsync();

        var repo = new AppointmentRepository(ctx);
        var appt = new Appointment
        {
            Id = Guid.NewGuid(),
            BarberId = barberId,
            UserId = userId,
            ServiceId = serviceId,
            AppointmentDate = date,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(9, 30, 0),
            Status = AppointmentStatus.Pending
        };
        await repo.AddAsync(appt);
        await ctx.SaveChangesAsync();

        var found = await repo.GetByIdAsync(appt.Id);
        found.Should().NotBeNull();
    }

    [Fact]
    public async Task AppointmentRepo_GetByBarberId_ShouldReturnAppointmentsOnDate()
    {
        using var ctx = CreateContext();
        var barberId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var date = new DateTime(2024, 5, 2);

        ctx.Users.Add(new User { Id = userId, Name = "U2", Email = "u2@t.com", PasswordHash = "hash" });
        ctx.Barbers.Add(new Barber { Id = barberId, Name = "B2", Email = "b2@t.com" });
        ctx.Services.Add(new Service { Id = serviceId, Name = "S2", DurationMinutes = 30, BasePrice = 50 });
        await ctx.SaveChangesAsync();

        var repo = new AppointmentRepository(ctx);
        await repo.AddAsync(new Appointment
        {
            Id = Guid.NewGuid(),
            BarberId = barberId,
            UserId = userId,
            ServiceId = serviceId,
            AppointmentDate = date,
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(10, 30, 0),
            Status = AppointmentStatus.Confirmed
        });
        await ctx.SaveChangesAsync();

        var results = await repo.GetByBarberIdAsync(barberId, date);
        results.Should().HaveCount(1);
    }

    [Fact]
    public async Task AppointmentRepo_GetByUserId_ShouldReturnUserAppointments()
    {
        using var ctx = CreateContext();
        var barberId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        ctx.Users.Add(new User { Id = userId, Name = "U3", Email = "u3@t.com", PasswordHash = "hash" });
        ctx.Barbers.Add(new Barber { Id = barberId, Name = "B3", Email = "b3@t.com" });
        ctx.Services.Add(new Service { Id = serviceId, Name = "S3", DurationMinutes = 30, BasePrice = 50 });
        await ctx.SaveChangesAsync();

        var repo = new AppointmentRepository(ctx);
        await repo.AddAsync(new Appointment
        {
            Id = Guid.NewGuid(),
            BarberId = barberId,
            UserId = userId,
            ServiceId = serviceId,
            AppointmentDate = new DateTime(2024, 5, 3),
            StartTime = new TimeSpan(11, 0, 0),
            EndTime = new TimeSpan(11, 30, 0),
            Status = AppointmentStatus.Confirmed
        });
        await ctx.SaveChangesAsync();

        var results = await repo.GetByUserIdAsync(userId);
        results.Should().HaveCount(1);
    }

    [Fact]
    public async Task AppointmentRepo_GetAll_ShouldReturnAll()
    {
        using var ctx = CreateContext();
        var barberId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        ctx.Users.Add(new User { Id = userId, Name = "U4", Email = "u4@t.com", PasswordHash = "hash" });
        ctx.Barbers.Add(new Barber { Id = barberId, Name = "B4", Email = "b4@t.com" });
        ctx.Services.Add(new Service { Id = serviceId, Name = "S4", DurationMinutes = 30, BasePrice = 50 });
        await ctx.SaveChangesAsync();

        var repo = new AppointmentRepository(ctx);
        await repo.AddAsync(new Appointment
        {
            Id = Guid.NewGuid(), BarberId = barberId, UserId = userId, ServiceId = serviceId,
            AppointmentDate = new DateTime(2024, 5, 4), StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(8, 30, 0), Status = AppointmentStatus.Pending
        });
        await ctx.SaveChangesAsync();

        var all = await repo.GetAllAsync();
        all.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AppointmentRepo_DeleteAsync_ShouldRemove()
    {
        using var ctx = CreateContext();
        var barberId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        ctx.Users.Add(new User { Id = userId, Name = "U5", Email = "u5@t.com", PasswordHash = "hash" });
        ctx.Barbers.Add(new Barber { Id = barberId, Name = "B5", Email = "b5@t.com" });
        ctx.Services.Add(new Service { Id = serviceId, Name = "S5", DurationMinutes = 30, BasePrice = 50 });
        await ctx.SaveChangesAsync();

        var repo = new AppointmentRepository(ctx);
        var appt = new Appointment
        {
            Id = Guid.NewGuid(), BarberId = barberId, UserId = userId, ServiceId = serviceId,
            AppointmentDate = new DateTime(2024, 5, 5), StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(9, 30, 0), Status = AppointmentStatus.Pending
        };
        await repo.AddAsync(appt);
        await ctx.SaveChangesAsync();

        await repo.DeleteAsync(appt.Id);
        await ctx.SaveChangesAsync();

        var found = await repo.GetByIdAsync(appt.Id);
        found.Should().BeNull();
    }
}
