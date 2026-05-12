using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SmartCutScheduler.Application.Features.BarberServices.AddService;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Repositories;
using Xunit;

public class AddServiceCommandHandler_AllCases
{
    [Fact]
    public async Task Handle_ShouldThrow_WhenBarberNotFound()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.Barbers.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Barber?)null);
        var handler = new AddServiceCommandHandler(unitOfWorkMock.Object);
        var command = new AddServiceCommand(Guid.NewGuid(), Guid.NewGuid(), null);
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenServiceNotFound()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var barber = new Barber { BarberServices = new List<BarberService>() };
        unitOfWorkMock.Setup(u => u.Barbers.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(barber);
        unitOfWorkMock.Setup(u => u.Services.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Service?)null);
        var handler = new AddServiceCommandHandler(unitOfWorkMock.Object);
        var command = new AddServiceCommand(Guid.NewGuid(), Guid.NewGuid(), null);
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenServiceAlreadyAdded()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var serviceId = Guid.NewGuid();
        var barber = new Barber { BarberServices = new List<BarberService> { new BarberService { ServiceId = serviceId } } };
        unitOfWorkMock.Setup(u => u.Barbers.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(barber);
        unitOfWorkMock.Setup(u => u.Services.GetByIdAsync(serviceId, It.IsAny<CancellationToken>())).ReturnsAsync(new Service { Id = serviceId });
        var handler = new AddServiceCommandHandler(unitOfWorkMock.Object);
        var command = new AddServiceCommand(Guid.NewGuid(), serviceId, null);
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldAddService_WhenValid()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var serviceId = Guid.NewGuid();
        var barber = new Barber { BarberServices = new List<BarberService>() };
        unitOfWorkMock.Setup(u => u.Barbers.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(barber);
        unitOfWorkMock.Setup(u => u.Services.GetByIdAsync(serviceId, It.IsAny<CancellationToken>())).ReturnsAsync(new Service { Id = serviceId });
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var handler = new AddServiceCommandHandler(unitOfWorkMock.Object);
        var command = new AddServiceCommand(Guid.NewGuid(), serviceId, null);
        var result = await handler.Handle(command, CancellationToken.None);
        result.Should().Be(serviceId);
    }
}
