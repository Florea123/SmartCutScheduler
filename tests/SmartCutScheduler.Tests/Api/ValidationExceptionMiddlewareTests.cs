using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Moq;
using SmartCutScheduler.Api.Middleware;
using Xunit;

namespace SmartCutScheduler.Tests.Api;

public class ValidationExceptionMiddleware_Tests
{
    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenNoException()
    {
        var context = new DefaultHttpContext();
        var nextCalled = false;
        RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };
        var middleware = new ValidationExceptionMiddleware(next);

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn400_WhenValidationExceptionThrown()
    {
        var errors = new[]
        {
            new FluentValidation.Results.ValidationFailure("Name", "Name is required"),
            new FluentValidation.Results.ValidationFailure("Email", "Email is invalid")
        };
        RequestDelegate next = (ctx) => throw new ValidationException(errors);
        var middleware = new ValidationExceptionMiddleware(next);

        var context = new DefaultHttpContext();
        context.Response.Body = new System.IO.MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(400);
        context.Response.ContentType.Should().Contain("application/json");
    }

    [Fact]
    public async Task InvokeAsync_ShouldGroupErrors_ByPropertyName()
    {
        var errors = new[]
        {
            new FluentValidation.Results.ValidationFailure("Name", "Too short"),
            new FluentValidation.Results.ValidationFailure("Name", "No uppercase"),
            new FluentValidation.Results.ValidationFailure("Email", "Invalid format")
        };
        RequestDelegate next = (ctx) => throw new ValidationException(errors);
        var middleware = new ValidationExceptionMiddleware(next);

        var context = new DefaultHttpContext();
        context.Response.Body = new System.IO.MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(400);
    }
}
