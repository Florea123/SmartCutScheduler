using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using SmartCutScheduler.Application.Features.Users.UpdateUserProfile;
using SmartCutScheduler.Domain.Repositories;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Application.Common.Interfaces;

public class UpdateUserProfileCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldThrowException_WhenUserNotFound()
    {
        // Arrange
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.Users.GetByIdAsync(It.IsAny<System.Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var fileStorageServiceMock = new Mock<IFileStorageService>();
        var handler = new UpdateUserProfileCommandHandler(unitOfWorkMock.Object, fileStorageServiceMock.Object);
        var command = new UpdateUserProfileCommand { UserId = System.Guid.NewGuid() };

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<System.Exception>().WithMessage("User not found");
    }

    // Alte teste pentru update reușit, sincronizare poze, etc.
}
