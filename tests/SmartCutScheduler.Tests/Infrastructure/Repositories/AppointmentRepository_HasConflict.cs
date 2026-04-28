using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Enums;
using SmartCutScheduler.Infrastructure.Persistence;
using SmartCutScheduler.Infrastructure.Repositories;
using Xunit;

namespace SmartCutScheduler.Tests.Infrastructure.Repositories;

public class AppointmentRepository_HasConflict
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task HasConflictAsync_ShouldReturnFalse_WhenNoAppointments()
    {
        using var context = CreateContext();
        var repo = new AppointmentRepository(context);
        var barberId = Guid.NewGuid();
        var date = new DateTime(2024, 3, 18);

        var result = await repo.HasConflictAsync(barberId, date, new TimeSpan(9, 0, 0), new TimeSpan(9, 30, 0));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasConflictAsync_ShouldReturnTrue_WhenOverlapExists()
    {
        using var context = CreateContext();
        var barberId = Guid.NewGuid();
        var date = new DateTime(2024, 3, 18);

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            BarberId = barberId,
            UserId = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            AppointmentDate = date,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(9, 30, 0),
            Status = AppointmentStatus.Confirmed
        };
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();

        var repo = new AppointmentRepository(context);
        var result = await repo.HasConflictAsync(barberId, date, new TimeSpan(9, 15, 0), new TimeSpan(9, 45, 0));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasConflictAsync_ShouldReturnFalse_WhenCancelledConflict()
    {
        using var context = CreateContext();
        var barberId = Guid.NewGuid();
        var date = new DateTime(2024, 3, 18);

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            BarberId = barberId,
            UserId = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            AppointmentDate = date,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(9, 30, 0),
            Status = AppointmentStatus.Cancelled
        };
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();

        var repo = new AppointmentRepository(context);
        var result = await repo.HasConflictAsync(barberId, date, new TimeSpan(9, 0, 0), new TimeSpan(9, 30, 0));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasConflictAsync_ShouldExclude_ExcludedAppointmentId()
    {
        using var context = CreateContext();
        var barberId = Guid.NewGuid();
        var date = new DateTime(2024, 3, 18);
        var apptId = Guid.NewGuid();

        var appointment = new Appointment
        {
            Id = apptId,
            BarberId = barberId,
            UserId = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            AppointmentDate = date,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(9, 30, 0),
            Status = AppointmentStatus.Confirmed
        };
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();

        var repo = new AppointmentRepository(context);
        var result = await repo.HasConflictAsync(barberId, date, new TimeSpan(9, 0, 0), new TimeSpan(9, 30, 0), excludeAppointmentId: apptId);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasConflictAsync_ShouldReturnTrue_WhenNewSlotFullyCoversExisting()
    {
        using var context = CreateContext();
        var barberId = Guid.NewGuid();
        var date = new DateTime(2024, 3, 18);

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            BarberId = barberId,
            UserId = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            AppointmentDate = date,
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(10, 30, 0),
            Status = AppointmentStatus.Confirmed
        };
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();

        var repo = new AppointmentRepository(context);
        // New slot covers the existing one entirely
        var result = await repo.HasConflictAsync(barberId, date, new TimeSpan(9, 45, 0), new TimeSpan(10, 45, 0));

        result.Should().BeTrue();
    }
}
