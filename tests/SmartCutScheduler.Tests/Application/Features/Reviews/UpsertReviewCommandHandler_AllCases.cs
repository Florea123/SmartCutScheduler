using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using SmartCutScheduler.Application.Features.Reviews.UpsertReview;
using SmartCutScheduler.Domain.Repositories;
using Xunit;

namespace SmartCutScheduler.Tests.Application.Features.Reviews;

public class UpsertReviewCommandHandler_AllCases
{
    private static UpsertReviewCommandHandler CreateHandler(
        HttpContext? httpContext,
        Mock<IReviewRepository>? repoMock = null)
    {
        if (repoMock == null)
        {
            repoMock = new Mock<IReviewRepository>();
            repoMock
                .Setup(r => r.AddOrUpdateReviewAsync(It.IsAny<SmartCutScheduler.Domain.Entities.Review>()))
                .Returns(Task.CompletedTask);
        }

        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(x => x.HttpContext).Returns(httpContext!);

        return new UpsertReviewCommandHandler(repoMock.Object, accessorMock.Object);
    }

    private static HttpContext AuthenticatedContext(string userId)
    {
        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "mock"));
        return ctx;
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenHttpContextIsNull()
    {
        var handler = CreateHandler(null);
        var cmd = new UpsertReviewCommand(Guid.NewGuid(), 5, "Nice");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.ToString().Should().Contain("Unauthorized");
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        var ctx = new DefaultHttpContext();
        // No identity set — IsAuthenticated is false
        var handler = CreateHandler(ctx);
        var cmd = new UpsertReviewCommand(Guid.NewGuid(), 4, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.ToString().Should().Contain("Unauthorized");
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserIdClaimIsInvalidGuid()
    {
        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "not-a-guid") }, "mock"));
        var handler = CreateHandler(ctx);
        var cmd = new UpsertReviewCommand(Guid.NewGuid(), 3, "Meh");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.ToString().Should().Contain("Unauthorized");
    }

    [Fact]
    public async Task Handle_ShouldReturnOk_WhenRequestIsValid()
    {
        var userId = Guid.NewGuid();
        var repoMock = new Mock<IReviewRepository>();
        repoMock
            .Setup(r => r.AddOrUpdateReviewAsync(It.IsAny<SmartCutScheduler.Domain.Entities.Review>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler(AuthenticatedContext(userId.ToString()), repoMock);
        var barberId = Guid.NewGuid();
        var cmd = new UpsertReviewCommand(barberId, 5, "Excellent!");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.ToString().Should().Contain("Ok");
        repoMock.Verify(r => r.AddOrUpdateReviewAsync(
            It.Is<SmartCutScheduler.Domain.Entities.Review>(rv =>
                rv.BarberId == barberId &&
                rv.UserId == userId &&
                rv.Rating == 5)), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPassCommentToRepository()
    {
        var userId = Guid.NewGuid();
        var repoMock = new Mock<IReviewRepository>();
        SmartCutScheduler.Domain.Entities.Review? savedReview = null;
        repoMock
            .Setup(r => r.AddOrUpdateReviewAsync(It.IsAny<SmartCutScheduler.Domain.Entities.Review>()))
            .Callback<SmartCutScheduler.Domain.Entities.Review>(r => savedReview = r)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler(AuthenticatedContext(userId.ToString()), repoMock);
        var cmd = new UpsertReviewCommand(Guid.NewGuid(), 2, "Could be better");

        await handler.Handle(cmd, CancellationToken.None);

        savedReview.Should().NotBeNull();
        savedReview!.Comment.Should().Be("Could be better");
        savedReview.Rating.Should().Be(2);
    }
}
