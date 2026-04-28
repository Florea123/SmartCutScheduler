using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using Moq;
using SmartCutScheduler.Application.Features.Users.UpdateUserProfile;
using Xunit;

namespace SmartCutScheduler.Tests.Application.Features.Users;

public class UpdateUserProfileCommandValidator_Tests
{
    private readonly UpdateUserProfileCommandValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenCommandIsValid()
    {
        var command = new UpdateUserProfileCommand { Name = "Test User", PhoneNumber = "0721000000" };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_WhenNameIsEmpty()
    {
        var command = new UpdateUserProfileCommand { Name = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Should_Fail_WhenNameExceeds100Chars()
    {
        var command = new UpdateUserProfileCommand { Name = new string('A', 101) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Should_Fail_WhenPhoneNumberExceeds20Chars()
    {
        var command = new UpdateUserProfileCommand { Name = "Test", PhoneNumber = new string('1', 21) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Fact]
    public void Should_Fail_WhenDescriptionExceeds500Chars()
    {
        var command = new UpdateUserProfileCommand { Name = "Test", Description = new string('A', 501) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Should_Pass_WhenProfileImageIsNull()
    {
        var command = new UpdateUserProfileCommand { Name = "Test", ProfileImage = null };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.ProfileImage);
    }

    [Fact]
    public void Should_Fail_WhenImageHasInvalidExtension()
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.gif");
        fileMock.Setup(f => f.Length).Returns(100);
        var command = new UpdateUserProfileCommand { Name = "Test", ProfileImage = fileMock.Object };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ProfileImage);
    }

    [Fact]
    public void Should_Fail_WhenImageExceeds2MB()
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("photo.jpg");
        fileMock.Setup(f => f.Length).Returns(3 * 1024 * 1024); // 3MB
        var command = new UpdateUserProfileCommand { Name = "Test", ProfileImage = fileMock.Object };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ProfileImage);
    }

    [Fact]
    public void Should_Pass_WhenImageIsValidPng()
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("avatar.png");
        fileMock.Setup(f => f.Length).Returns(500 * 1024); // 500KB
        var command = new UpdateUserProfileCommand { Name = "Test", ProfileImage = fileMock.Object };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.ProfileImage);
    }
}
