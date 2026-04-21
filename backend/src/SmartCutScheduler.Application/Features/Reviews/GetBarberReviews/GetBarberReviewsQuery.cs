using MediatR;
using Microsoft.AspNetCore.Http;

namespace SmartCutScheduler.Application.Features.Reviews.GetBarberReviews;

public record GetBarberReviewsQuery(Guid BarberId) : IRequest<IResult>;
