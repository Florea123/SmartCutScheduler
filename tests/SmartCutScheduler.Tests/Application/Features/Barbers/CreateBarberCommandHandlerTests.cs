using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using SmartCutScheduler.Application.Features.Barbers.CreateBarber;
using SmartCutScheduler.Domain.Repositories;
using SmartCutScheduler.Domain.Entities;
using Microsoft.AspNetCore.Identity;

public class CreateBarberCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldThrow_WhenEmailAlreadyExists()
    {
        // Arrange
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.Users.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new User());
        var passwordHasher = new PasswordHasher<User>();
        var handler = new CreateBarberCommandHandler(unitOfWorkMock.Object, passwordHasher);
        var command = new CreateBarberCommand("Barber", "test@test.com", "123", "pass", "desc", null);

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<System.InvalidOperationException>().WithMessage("Email-ul este deja folosit.");
    }

    // Alte teste pentru creare reușită, input invalid, etc.
}
