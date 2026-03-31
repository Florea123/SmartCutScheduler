using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using SmartCutScheduler.Application.Features.Reviews;
using SmartCutScheduler.Application.Features.Reviews.GetBarberReviews;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Repositories;
using Xunit;

namespace SmartCutScheduler.Tests.Application.Features.Reviews;

public class GetBarberReviewsQueryHandler_AllCases
{
    private static GetBarberReviewsQueryHandler CreateHandler(
        IEnumerable<Review> reviews, Guid barberId)
    {
        var repoMock = new Mock<IReviewRepository>();
        repoMock
            .Setup(r => r.GetReviewsForBarberAsync(barberId))
            .ReturnsAsync(reviews);
        return new GetBarberReviewsQueryHandler(repoMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnOk_WithEmptyList_WhenNoReviews()
    {
        var barberId = Guid.NewGuid();
        var handler = CreateHandler(new List<Review>(), barberId);
        var query = new GetBarberReviewsQuery(barberId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.ToString().Should().Contain("Ok");
    }

    [Fact]
    public async Task Handle_ShouldReturnOk_WithReviews_OrderedByDateDesc()
    {
        var barberId = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        var older = new Review
        {
            Id = Guid.NewGuid(),
            BarberId = barberId,
            UserId = userId1,
            Rating = 3,
            Comment = "OK",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            User = new User { Id = userId1, Name = "Alice", Email = "alice@test.com" }
        };
        var newer = new Review
        {
            Id = Guid.NewGuid(),
            BarberId = barberId,
            UserId = userId2,
            Rating = 5,
            Comment = "Great!",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            User = new User { Id = userId2, Name = "Bob", Email = "bob@test.com" }
        };

        var handler = CreateHandler(new List<Review> { older, newer }, barberId);
        var query = new GetBarberReviewsQuery(barberId);

        var result = await handler.Handle(query, CancellationToken.None);

        // Result is Results.Ok — unwrap via reflection to verify ordering
        var okResult = result as Ok<System.Collections.Generic.List<ReviewDto>>;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().NotBeNull();
        okResult.Value!.Count.Should().Be(2);
        // Newest first
        okResult.Value[0].Rating.Should().Be(5);
        okResult.Value[1].Rating.Should().Be(3);
    }

    [Fact]
    public async Task Handle_ShouldFallbackToDefaultName_WhenUserIsNull()
    {
        var barberId = Guid.NewGuid();
        var review = new Review
        {
            Id = Guid.NewGuid(),
            BarberId = barberId,
            UserId = Guid.NewGuid(),
            Rating = 4,
            Comment = null,
            CreatedAt = DateTime.UtcNow,
            User = null   // navigation property not loaded
        };

        var handler = CreateHandler(new List<Review> { review }, barberId);
        var result = await handler.Handle(new GetBarberReviewsQuery(barberId), CancellationToken.None);

        var okResult = result as Ok<System.Collections.Generic.List<ReviewDto>>;
        okResult.Should().NotBeNull();
        okResult!.Value![0].UserName.Should().Be("Utilizator");
    }
}
