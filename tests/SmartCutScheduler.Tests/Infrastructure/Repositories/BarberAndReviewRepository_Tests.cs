using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Enums;
using SmartCutScheduler.Infrastructure.Persistence;
using SmartCutScheduler.Infrastructure.Repositories;
using Xunit;

namespace SmartCutScheduler.Tests.Infrastructure.Repositories;

public class BarberAndReviewRepository_Tests
{
    private static AppDbContext CreateContext() =>
        new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    // ─────────────────────────── BarberRepository ────────────────────────────

    [Fact]
    public async Task BarberRepo_AddAndGetById_ShouldWork()
    {
        using var ctx = CreateContext();
        var repo = new BarberRepository(ctx);
        var barber = new Barber { Id = Guid.NewGuid(), Name = "Tony", Email = "tony@test.com" };
        await repo.AddAsync(barber);
        await ctx.SaveChangesAsync();

        var found = await repo.GetByIdAsync(barber.Id);
        found.Should().NotBeNull();
        found!.Name.Should().Be("Tony");
    }

    [Fact]
    public async Task BarberRepo_GetAll_ExcludesInactive()
    {
        using var ctx = CreateContext();
        var repo = new BarberRepository(ctx);
        await repo.AddAsync(new Barber { Id = Guid.NewGuid(), Name = "A", Email = "a@t.com", IsActive = true });
        await repo.AddAsync(new Barber { Id = Guid.NewGuid(), Name = "B", Email = "b@t.com", IsActive = false });
        await ctx.SaveChangesAsync();

        var active = await repo.GetAllAsync(includeInactive: false);
        active.Should().HaveCount(1);
    }

    [Fact]
    public async Task BarberRepo_GetAll_IncludesInactive_WhenFlagSet()
    {
        using var ctx = CreateContext();
        var repo = new BarberRepository(ctx);
        await repo.AddAsync(new Barber { Id = Guid.NewGuid(), Name = "A", Email = "a@t.com", IsActive = true });
        await repo.AddAsync(new Barber { Id = Guid.NewGuid(), Name = "B", Email = "b@t.com", IsActive = false });
        await ctx.SaveChangesAsync();

        var all = await repo.GetAllAsync(includeInactive: true);
        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task BarberRepo_Update_ShouldPersistChanges()
    {
        using var ctx = CreateContext();
        var repo = new BarberRepository(ctx);
        var barber = new Barber { Id = Guid.NewGuid(), Name = "Old", Email = "old@t.com" };
        await repo.AddAsync(barber);
        await ctx.SaveChangesAsync();

        barber.Name = "New";
        await repo.UpdateAsync(barber);
        await ctx.SaveChangesAsync();

        var found = await repo.GetByIdAsync(barber.Id);
        found!.Name.Should().Be("New");
    }

    [Fact]
    public async Task BarberRepo_Delete_ShouldRemove()
    {
        using var ctx = CreateContext();
        var repo = new BarberRepository(ctx);
        var barber = new Barber { Id = Guid.NewGuid(), Name = "Del", Email = "del@t.com" };
        await repo.AddAsync(barber);
        await ctx.SaveChangesAsync();

        await repo.DeleteAsync(barber.Id);
        await ctx.SaveChangesAsync();

        var found = await repo.GetByIdAsync(barber.Id);
        found.Should().BeNull();
    }

    [Fact]
    public async Task BarberRepo_GetById_ShouldReturnNull_WhenNotFound()
    {
        using var ctx = CreateContext();
        var repo = new BarberRepository(ctx);
        var found = await repo.GetByIdAsync(Guid.NewGuid());
        found.Should().BeNull();
    }

    // ─────────────────────────── ReviewRepository ────────────────────────────

    private static (User user, Barber barber) SeedUserAndBarber(AppDbContext ctx)
    {
        var user = new User { Id = Guid.NewGuid(), Name = "U", Email = "u@t.com", PasswordHash = "h" };
        var barber = new Barber { Id = Guid.NewGuid(), Name = "B", Email = "b@t.com" };
        ctx.Users.Add(user);
        ctx.Barbers.Add(barber);
        ctx.SaveChanges();
        return (user, barber);
    }

    [Fact]
    public async Task ReviewRepo_GetReviewsForBarber_ShouldReturnReviews()
    {
        using var ctx = CreateContext();
        var (user, barber) = SeedUserAndBarber(ctx);
        ctx.Reviews.Add(new Review { Id = Guid.NewGuid(), BarberId = barber.Id, UserId = user.Id, Rating = 5 });
        await ctx.SaveChangesAsync();

        var repo = new ReviewRepository(ctx);
        var reviews = await repo.GetReviewsForBarberAsync(barber.Id);
        reviews.Should().HaveCount(1);
    }

    [Fact]
    public async Task ReviewRepo_GetReviewByUserAndBarber_ShouldReturnNull_WhenNotFound()
    {
        using var ctx = CreateContext();
        var repo = new ReviewRepository(ctx);
        var found = await repo.GetReviewByUserAndBarberAsync(Guid.NewGuid(), Guid.NewGuid());
        found.Should().BeNull();
    }

    [Fact]
    public async Task ReviewRepo_AddOrUpdateReview_ShouldAdd_WhenNotExists()
    {
        using var ctx = CreateContext();
        var (user, barber) = SeedUserAndBarber(ctx);

        var repo = new ReviewRepository(ctx);
        var review = new Review { Id = Guid.NewGuid(), BarberId = barber.Id, UserId = user.Id, Rating = 4, Comment = "Good" };
        await repo.AddOrUpdateReviewAsync(review);

        var found = await repo.GetReviewByUserAndBarberAsync(user.Id, barber.Id);
        found.Should().NotBeNull();
        found!.Rating.Should().Be(4);
    }

    [Fact]
    public async Task ReviewRepo_AddOrUpdateReview_ShouldUpdate_WhenExists()
    {
        using var ctx = CreateContext();
        var (user, barber) = SeedUserAndBarber(ctx);

        ctx.Reviews.Add(new Review { Id = Guid.NewGuid(), BarberId = barber.Id, UserId = user.Id, Rating = 3 });
        await ctx.SaveChangesAsync();

        var repo = new ReviewRepository(ctx);
        var updatedReview = new Review { Id = Guid.NewGuid(), BarberId = barber.Id, UserId = user.Id, Rating = 5, Comment = "Updated" };
        await repo.AddOrUpdateReviewAsync(updatedReview);

        var found = await repo.GetReviewByUserAndBarberAsync(user.Id, barber.Id);
        found!.Rating.Should().Be(5);
    }
}
