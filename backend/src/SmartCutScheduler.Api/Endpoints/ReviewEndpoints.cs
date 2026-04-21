using MediatR;
using SmartCutScheduler.Application.Features.Reviews.GetBarberReviews;
using SmartCutScheduler.Application.Features.Reviews.UpsertReview;

namespace SmartCutScheduler.Api.Endpoints;

public static class ReviewEndpoints
{
    public static void MapReviewEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/reviews")
            .WithTags("Reviews");

        group.MapGet("/barber/{barberId:guid}", async (Guid barberId, IMediator mediator) =>
            await mediator.Send(new GetBarberReviewsQuery(barberId)));

        group.MapPost("", async (UpsertReviewCommand cmd, IMediator mediator) =>
            await mediator.Send(cmd))
            .RequireAuthorization();
    }
}

