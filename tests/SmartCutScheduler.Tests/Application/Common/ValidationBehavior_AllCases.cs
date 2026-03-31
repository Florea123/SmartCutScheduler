using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Moq;
using SmartCutScheduler.Application.Common.Behaviors;
using Xunit;

public class ValidationBehavior_AllCases
{
    private class DummyRequest : IRequest<string> { public string? Value { get; set; } }
    private class DummyValidator : AbstractValidator<DummyRequest>
    {
        public DummyValidator() { RuleFor(x => x.Value).NotEmpty(); }
    }

    [Fact]
    public async Task Should_ThrowValidationException_WhenValidationFails()
    {
        var validators = new[] { new DummyValidator() };
        var behavior = new ValidationBehavior<DummyRequest, string>(validators);
        var request = new DummyRequest { Value = null };
        var next = new Mock<RequestHandlerDelegate<string>>();
        await Assert.ThrowsAsync<ValidationException>(() => behavior.Handle(request, next.Object, CancellationToken.None));
    }

    [Fact]
    public async Task Should_CallNext_WhenValidationPasses()
    {
        var validators = new[] { new DummyValidator() };
        var behavior = new ValidationBehavior<DummyRequest, string>(validators);
        var request = new DummyRequest { Value = "ok" };
        var next = new Mock<RequestHandlerDelegate<string>>();
        next.Setup(n => n()).ReturnsAsync("success");
        var result = await behavior.Handle(request, next.Object, CancellationToken.None);
        result.Should().Be("success");
    }

    [Fact]
    public async Task Should_SkipValidation_WhenNoValidators()
    {
        var validators = new DummyValidator[0];
        var behavior = new ValidationBehavior<DummyRequest, string>(validators);
        var request = new DummyRequest { Value = null };
        var next = new Mock<RequestHandlerDelegate<string>>();
        next.Setup(n => n()).ReturnsAsync("no-validation");
        var result = await behavior.Handle(request, next.Object, CancellationToken.None);
        result.Should().Be("no-validation");
    }
}
