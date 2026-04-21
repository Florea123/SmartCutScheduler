using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SmartCutScheduler.Application.Features.BarberServices.CreateCustomService;
using SmartCutScheduler.Application.Features.BarberServices.GetMyServices;
using SmartCutScheduler.Application.Features.BarberServices.RemoveService;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Repositories;
using Xunit;

namespace SmartCutScheduler.Tests.Application.Features.BarberServices;

public class RemoveServiceCommandHandler_AllCases
{
    [Fact]
    public async Task Handle_ShouldThrow_WhenBarberNotFound()
    {
        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(u => u.Barbers.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((Barber?)null);
        var handler = new RemoveServiceCommandHandler(uowMock.Object);
        var cmd = new RemoveServiceCommand(Guid.NewGuid(), Guid.NewGuid());

        var act = () => handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("Frizer nu a fost găsit.");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenServiceNotFound()
    {
        var barberId = Guid.NewGuid();
        var barber = new Barber
        {
            Id = barberId,
            Name = "Test",
            BarberServices = new List<BarberService>()  // no services
        };
        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(u => u.Barbers.GetByIdAsync(barberId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(barber);
        var handler = new RemoveServiceCommandHandler(uowMock.Object);
        var cmd = new RemoveServiceCommand(barberId, Guid.NewGuid());

        var act = () => handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("Serviciul nu a fost găsit.");
    }

    [Fact]
    public async Task Handle_ShouldReturnTrue_WhenValid()
    {
        var barberId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var svc = new BarberService { BarberId = barberId, ServiceId = serviceId };
        var barber = new Barber
        {
            Id = barberId,
            Name = "Test",
            BarberServices = new List<BarberService> { svc }
        };
        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(u => u.Barbers.GetByIdAsync(barberId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(barber);
        uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(1);
        var handler = new RemoveServiceCommandHandler(uowMock.Object);
        var cmd = new RemoveServiceCommand(barberId, serviceId);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().BeTrue();
        barber.BarberServices.Should().BeEmpty();
    }
}

public class CreateCustomServiceCommandHandler_AllCases
{
    [Fact]
    public async Task Handle_ShouldThrow_WhenBarberNotFound()
    {
        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(u => u.Barbers.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((Barber?)null);
        var handler = new CreateCustomServiceCommandHandler(uowMock.Object);
        var cmd = new CreateCustomServiceCommand(Guid.NewGuid(), "Cut", null, 30, 50m);

        var act = () => handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("Frizer nu a fost găsit.");
    }

    [Fact]
    public async Task Handle_ShouldCreateServiceAndReturnId_WhenValid()
    {
        var barberId = Guid.NewGuid();
        var barber = new Barber
        {
            Id = barberId,
            Name = "Test",
            BarberServices = new List<BarberService>()
        };
        Service? addedService = null;
        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(u => u.Barbers.GetByIdAsync(barberId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(barber);
        uowMock.Setup(u => u.Services.AddAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()))
               .Callback<Service, CancellationToken>((s, _) => addedService = s)
               .Returns(Task.CompletedTask);
        uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(1);
        var handler = new CreateCustomServiceCommandHandler(uowMock.Object);
        var cmd = new CreateCustomServiceCommand(barberId, "Fade", "Classic fade", 45, 60m);

        var resultId = await handler.Handle(cmd, CancellationToken.None);

        resultId.Should().NotBeEmpty();
        addedService.Should().NotBeNull();
        addedService!.Name.Should().Be("Fade");
        addedService.BasePrice.Should().Be(60m);
        barber.BarberServices.Should().HaveCount(1);
    }
}

public class GetMyServicesQueryHandler_AllCases
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenBarberDoesNotExist()
    {
        var repoMock = new Mock<IBarberRepository>();
        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Barber?)null);
        var handler = new GetMyServicesQueryHandler(repoMock.Object);

        var result = await handler.Handle(
            new GetMyServicesQuery(Guid.NewGuid()), CancellationToken.None);

        result.ToString().Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_ShouldReturnOk_WithServicesList()
    {
        var barberId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var service = new Service
        {
            Id = serviceId, Name = "Tuns", Description = "Classic", DurationMinutes = 30, BasePrice = 40m, IsActive = true
        };
        var barber = new Barber
        {
            Id = barberId,
            Name = "Test",
            BarberServices = new List<BarberService>
            {
                new BarberService { BarberId = barberId, ServiceId = serviceId, Service = service, CustomPrice = 45m }
            }
        };
        var repoMock = new Mock<IBarberRepository>();
        repoMock.Setup(r => r.GetByIdAsync(barberId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(barber);
        var handler = new GetMyServicesQueryHandler(repoMock.Object);

        var result = await handler.Handle(new GetMyServicesQuery(barberId), CancellationToken.None);

        result.ToString().Should().Contain("Ok");
    }
}
