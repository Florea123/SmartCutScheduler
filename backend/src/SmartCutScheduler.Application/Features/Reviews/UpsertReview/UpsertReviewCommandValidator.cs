using FluentValidation;

namespace SmartCutScheduler.Application.Features.Reviews.UpsertReview;

public class UpsertReviewCommandValidator : AbstractValidator<UpsertReviewCommand>
{
    public UpsertReviewCommandValidator()
    {
        RuleFor(x => x.BarberId).NotEmpty();
        RuleFor(x => x.Rating).InclusiveBetween(1, 5)
            .WithMessage("Rating-ul trebuie să fie între 1 și 5.");
        RuleFor(x => x.Comment).MaximumLength(1000)
            .WithMessage("Comentariul nu poate depăși 1000 de caractere.");
    }
}
