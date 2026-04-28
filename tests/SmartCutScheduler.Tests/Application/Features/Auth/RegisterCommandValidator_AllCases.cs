using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using Moq;
using SmartCutScheduler.Application.Features.Auth.Register;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Repositories;
using Xunit;

namespace SmartCutScheduler.Tests.Application.Features.Auth;

public class RegisterCommandValidator_AllCases
{
    private static RegisterCommandValidator CreateValidator(User? existingUser = null)
    {
        var userRepoMock = new Mock<IUserRepository>();
        userRepoMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        return new RegisterCommandValidator(userRepoMock.Object);
    }

    [Fact]
    public async Task Validate_ShouldPass_WhenAllFieldsValid()
    {
        var validator = CreateValidator(null);
        var command = new RegisterCommand("John Doe", "john@example.com", "Password1!", "0722");

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenNameIsEmpty()
    {
        var validator = CreateValidator(null);
        var command = new RegisterCommand("", "john@example.com", "Password1!", null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenEmailIsInvalid()
    {
        var validator = CreateValidator(null);
        var command = new RegisterCommand("John", "not-an-email", "Password1!", null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenEmailAlreadyRegistered()
    {
        var existingUser = new User { Email = "taken@example.com" };
        var validator = CreateValidator(existingUser);
        var command = new RegisterCommand("John", "taken@example.com", "Password1!", null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Email already registered.");
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenPasswordTooShort()
    {
        var validator = CreateValidator(null);
        var command = new RegisterCommand("John", "john@example.com", "Abc1!", null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenPasswordHasNoUppercase()
    {
        var validator = CreateValidator(null);
        var command = new RegisterCommand("John", "john@example.com", "password1!", null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("uppercase"));
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenPasswordHasNoLowercase()
    {
        var validator = CreateValidator(null);
        var command = new RegisterCommand("John", "john@example.com", "PASSWORD1!", null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("lowercase"));
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenPasswordHasNoDigit()
    {
        var validator = CreateValidator(null);
        var command = new RegisterCommand("John", "john@example.com", "Password!!", null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("digit"));
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenPasswordHasNoSpecialChar()
    {
        var validator = CreateValidator(null);
        var command = new RegisterCommand("John", "john@example.com", "Password123", null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("non-alphanumeric"));
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenPhoneNumberTooLong()
    {
        var validator = CreateValidator(null);
        var command = new RegisterCommand("John", "john@example.com", "Password1!", new string('1', 25));

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PhoneNumber");
    }

    [Fact]
    public async Task Validate_ShouldPass_WhenPhoneNumberIsNull()
    {
        var validator = CreateValidator(null);
        var command = new RegisterCommand("John", "john@example.com", "Password1!", null);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
