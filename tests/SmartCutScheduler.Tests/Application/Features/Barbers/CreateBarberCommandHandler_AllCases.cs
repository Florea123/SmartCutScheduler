using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using SmartCutScheduler.Application.Features.Barbers.CreateBarber;
using SmartCutScheduler.Domain.Repositories;
using SmartCutScheduler.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System;

public class CreateBarberCommandHandler_AllCases
{
    [Fact]
    public async Task Handle_ShouldThrow_WhenEmailAlreadyExists()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.Users.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new User());
        var passwordHasher = new PasswordHasher<User>();
        var handler = new CreateBarberCommandHandler(unitOfWorkMock.Object, passwordHasher);
        var command = new CreateBarberCommand("Barber", "test@test.com", "123", "pass", "desc", null);
        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Email-ul este deja folosit.");
    }

    [Fact]
    public async Task Handle_ShouldCreateBarber_WhenValid()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.Users.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User)null);
        unitOfWorkMock.Setup(u => u.Users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWorkMock.Setup(u => u.Barbers.AddAsync(It.IsAny<Barber>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var passwordHasher = new PasswordHasher<User>();
        var handler = new CreateBarberCommandHandler(unitOfWorkMock.Object, passwordHasher);
        var command = new CreateBarberCommand("Barber", "test@test.com", "123", "pass", "desc", null);
        var result = await handler.Handle(command, CancellationToken.None);
        result.Should().NotBeEmpty();
    }
}
