using MediatR;
using Microsoft.AspNetCore.Http;
using SmartCutScheduler.Domain.Repositories;

namespace SmartCutScheduler.Application.Features.Reviews.GetBarberReviews;

public class GetBarberReviewsQueryHandler(IReviewRepository reviewRepository)
    : IRequestHandler<GetBarberReviewsQuery, IResult>
{
    public async Task<IResult> Handle(GetBarberReviewsQuery request, CancellationToken cancellationToken)
    {
        var reviews = await reviewRepository.GetReviewsForBarberAsync(request.BarberId);

        var result = reviews
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewDto(
                r.Id,
                r.BarberId,
                r.UserId,
                r.User?.Name ?? "Utilizator",
                r.Rating,
                r.Comment,
                r.CreatedAt,
                r.UpdatedAt
            ))
            .ToList();

        return Results.Ok(result);
    }
}
