using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using SmartCutScheduler.Application.Common.Interfaces;
using SmartCutScheduler.Application.Features.Users.UpdateUserProfile;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Enums;
using SmartCutScheduler.Domain.Repositories;
using Xunit;

namespace SmartCutScheduler.Tests.Application.Features.Users;

public class UpdateUserProfileCommandHandler_Extended
{
    [Fact]
    public async Task Handle_ShouldUpdateBarberDescription_WhenUserIsBarberAndDescriptionProvided()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Role = UserRole.Barber };
        var barber = new Barber { Id = userId, Description = "old" };

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.Users.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        unitOfWorkMock.Setup(u => u.Barbers.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(barber);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var fileStorageMock = new Mock<IFileStorageService>();
        var handler = new UpdateUserProfileCommandHandler(unitOfWorkMock.Object, fileStorageMock.Object);
        var command = new UpdateUserProfileCommand { UserId = userId, Name = "Alex", Description = "new description" };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();
        barber.Description.Should().Be("new description");
    }

    [Fact]
    public async Task Handle_ShouldSaveProfileImage_WhenImageProvided()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Role = UserRole.Customer };

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.Users.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var formFileMock = new Mock<IFormFile>();
        var fileStorageMock = new Mock<IFileStorageService>();
        fileStorageMock.Setup(f => f.SaveProfileImageAsync(userId, formFileMock.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync("images/pic.jpg");

        var handler = new UpdateUserProfileCommandHandler(unitOfWorkMock.Object, fileStorageMock.Object);
        var command = new UpdateUserProfileCommand { UserId = userId, Name = "Test", ProfileImage = formFileMock.Object };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();
        user.ProfilePictureUrl.Should().Be("images/pic.jpg");
    }

    [Fact]
    public async Task Handle_ShouldSyncBarberPhoto_WhenBarberImageUpdated()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Role = UserRole.Barber };
        var barber = new Barber { Id = userId, PhotoUrl = null };

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.Users.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        unitOfWorkMock.Setup(u => u.Barbers.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(barber);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var formFileMock = new Mock<IFormFile>();
        var fileStorageMock = new Mock<IFileStorageService>();
        fileStorageMock.Setup(f => f.SaveProfileImageAsync(userId, formFileMock.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync("images/barber.jpg");

        var handler = new UpdateUserProfileCommandHandler(unitOfWorkMock.Object, fileStorageMock.Object);
        var command = new UpdateUserProfileCommand { UserId = userId, Name = "Barber", ProfileImage = formFileMock.Object };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();
        barber.PhotoUrl.Should().NotBeNull();
    }
}
