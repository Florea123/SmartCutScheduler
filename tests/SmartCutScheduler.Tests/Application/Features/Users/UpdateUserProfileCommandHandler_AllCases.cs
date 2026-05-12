using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using SmartCutScheduler.Application.Features.Users.UpdateUserProfile;
using SmartCutScheduler.Domain.Repositories;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Application.Common.Interfaces;
using System;

public class UpdateUserProfileCommandHandler_AllCases
{
    [Fact]
    public async Task Handle_ShouldThrow_WhenUserNotFound()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.Users.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var fileStorageServiceMock = new Mock<IFileStorageService>();
        var handler = new UpdateUserProfileCommandHandler(unitOfWorkMock.Object, fileStorageServiceMock.Object);
        var command = new UpdateUserProfileCommand { UserId = Guid.NewGuid() };
        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<Exception>().WithMessage("User not found");
    }

    [Fact]
    public async Task Handle_ShouldUpdateUser_WhenValid()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var user = new User { Role = SmartCutScheduler.Domain.Enums.UserRole.Customer };
        unitOfWorkMock.Setup(u => u.Users.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var fileStorageServiceMock = new Mock<IFileStorageService>();
        var handler = new UpdateUserProfileCommandHandler(unitOfWorkMock.Object, fileStorageServiceMock.Object);
        var command = new UpdateUserProfileCommand { UserId = Guid.NewGuid(), Name = "Test", PhoneNumber = "123" };
        var result = await handler.Handle(command, CancellationToken.None);
        result.Should().BeTrue();
        user.Name.Should().Be("Test");
        user.PhoneNumber.Should().Be("123");
    }
    // Alte teste: update cu imagine, sincronizare barber, etc.
}
