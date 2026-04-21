using System;
using FluentAssertions;
using FluentValidation.TestHelper;
using SmartCutScheduler.Application.Features.Reviews.UpsertReview;
using Xunit;

namespace SmartCutScheduler.Tests.Application.Features.Reviews;

public class UpsertReviewCommandValidator_AllCases
{
    private readonly UpsertReviewCommandValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenCommandIsValid()
    {
        var cmd = new UpsertReviewCommand(Guid.NewGuid(), 4, "Good service");
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Pass_WhenCommentIsNull()
    {
        var cmd = new UpsertReviewCommand(Guid.NewGuid(), 5, null);
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_WhenBarberId_IsEmpty()
    {
        var cmd = new UpsertReviewCommand(Guid.Empty, 3, "OK");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.BarberId);
    }

    [Fact]
    public void Should_Fail_WhenRating_IsZero()
    {
        var cmd = new UpsertReviewCommand(Guid.NewGuid(), 0, "Bad");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Rating)
              .WithErrorMessage("Rating-ul trebuie să fie între 1 și 5.");
    }

    [Fact]
    public void Should_Fail_WhenRating_IsSix()
    {
        var cmd = new UpsertReviewCommand(Guid.NewGuid(), 6, null);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Rating)
              .WithErrorMessage("Rating-ul trebuie să fie între 1 și 5.");
    }

    [Fact]
    public void Should_Fail_WhenComment_ExceedsMaxLength()
    {
        var longComment = new string('a', 1001);
        var cmd = new UpsertReviewCommand(Guid.NewGuid(), 3, longComment);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Comment)
              .WithErrorMessage("Comentariul nu poate depăși 1000 de caractere.");
    }

    [Fact]
    public void Should_Pass_WhenComment_IsExactlyMaxLength()
    {
        var maxComment = new string('a', 1000);
        var cmd = new UpsertReviewCommand(Guid.NewGuid(), 1, maxComment);
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.Comment);
    }
}
