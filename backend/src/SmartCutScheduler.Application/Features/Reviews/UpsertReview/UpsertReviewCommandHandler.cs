using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Repositories;

namespace SmartCutScheduler.Application.Features.Reviews.UpsertReview;

public class UpsertReviewCommandHandler(
    IReviewRepository reviewRepository,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<UpsertReviewCommand, IResult>
{
    public async Task<IResult> Handle(UpsertReviewCommand request, CancellationToken cancellationToken)
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx is null || ctx.User.Identity?.IsAuthenticated != true)
            return Results.Unauthorized();

        var userIdClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Results.Unauthorized();

        var review = new Review
        {
            Id = Guid.NewGuid(),
            BarberId = request.BarberId,
            UserId = userId,
            Rating = request.Rating,
            Comment = request.Comment
        };

        await reviewRepository.AddOrUpdateReviewAsync(review);

        return Results.Ok();
    }
}
