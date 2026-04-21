using MediatR;
using Microsoft.AspNetCore.Http;

namespace SmartCutScheduler.Application.Features.Reviews.UpsertReview;

public record UpsertReviewCommand(
    Guid BarberId,
    int Rating,
    string? Comment
) : IRequest<IResult>;
