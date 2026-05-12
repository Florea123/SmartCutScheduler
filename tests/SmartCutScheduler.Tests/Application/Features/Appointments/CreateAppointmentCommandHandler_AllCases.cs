using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using SmartCutScheduler.Application.Features.Appointments.CreateAppointment;
using SmartCutScheduler.Domain.Repositories;
using SmartCutScheduler.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Collections.Generic;

public class CreateAppointmentCommandHandler_AllCases
{
    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        var handler = new CreateAppointmentCommandHandler(unitOfWorkMock.Object, httpContextAccessorMock.Object);
        var command = new CreateAppointmentCommand(Guid.NewGuid(), Guid.NewGuid(), DateTime.Today, "10:00", null);
        var result = await handler.Handle(command, CancellationToken.None);
        result.ToString().Should().Contain("Unauthorized");
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserIdInvalid()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "not-a-guid") }, "mock"));
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        var handler = new CreateAppointmentCommandHandler(unitOfWorkMock.Object, httpContextAccessorMock.Object);
        var command = new CreateAppointmentCommand(Guid.NewGuid(), Guid.NewGuid(), DateTime.Today, "10:00", null);
        var result = await handler.Handle(command, CancellationToken.None);
        result.ToString().Should().Contain("Unauthorized");
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenBarberNotFound()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.Barbers.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Barber?)null);
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }, "mock"));
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        var handler = new CreateAppointmentCommandHandler(unitOfWorkMock.Object, httpContextAccessorMock.Object);
        var command = new CreateAppointmentCommand(Guid.NewGuid(), Guid.NewGuid(), DateTime.Today, "10:00", null);
        var result = await handler.Handle(command, CancellationToken.None);
        result.ToString().Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenServiceNotFound()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var barber = new Barber { IsActive = true, BarberServices = new List<BarberService>() };
        unitOfWorkMock.Setup(u => u.Barbers.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(barber);
        unitOfWorkMock.Setup(u => u.Services.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Service)null);
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }, "mock"));
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        var handler = new CreateAppointmentCommandHandler(unitOfWorkMock.Object, httpContextAccessorMock.Object);
        var command = new CreateAppointmentCommand(Guid.NewGuid(), Guid.NewGuid(), DateTime.Today, "10:00", null);
        var result = await handler.Handle(command, CancellationToken.None);
        result.ToString().Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenBarberDoesNotOfferService()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var barber = new Barber { IsActive = true, BarberServices = new List<BarberService>() };
        var service = new Service { IsActive = true, DurationMinutes = 30 };
        unitOfWorkMock.Setup(u => u.Barbers.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(barber);
        unitOfWorkMock.Setup(u => u.Services.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(service);
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }, "mock"));
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        var handler = new CreateAppointmentCommandHandler(unitOfWorkMock.Object, httpContextAccessorMock.Object);
        var command = new CreateAppointmentCommand(Guid.NewGuid(), Guid.NewGuid(), DateTime.Today, "10:00", null);
        var result = await handler.Handle(command, CancellationToken.None);
        result.ToString().Should().Contain("BadRequest");
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenStartTimeInvalid()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var barber = new Barber { IsActive = true, BarberServices = new List<BarberService> { new BarberService { ServiceId = Guid.NewGuid() } } };
        var service = new Service { IsActive = true, DurationMinutes = 30 };
        unitOfWorkMock.Setup(u => u.Barbers.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(barber);
        unitOfWorkMock.Setup(u => u.Services.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(service);
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }, "mock"));
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        var handler = new CreateAppointmentCommandHandler(unitOfWorkMock.Object, httpContextAccessorMock.Object);
        var command = new CreateAppointmentCommand(Guid.NewGuid(), Guid.NewGuid(), DateTime.Today, "invalid", null);
        var result = await handler.Handle(command, CancellationToken.None);
        result.ToString().Should().Contain("BadRequest");
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenSlotNotAvailable()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var barber = new Barber { IsActive = true, BarberServices = new List<BarberService> { new BarberService { ServiceId = Guid.NewGuid() } } };
        var service = new Service { IsActive = true, DurationMinutes = 30 };
        unitOfWorkMock.Setup(u => u.Barbers.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(barber);
        unitOfWorkMock.Setup(u => u.Services.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(service);
        unitOfWorkMock.Setup(u => u.Appointments.HasConflictAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }, "mock"));
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        var handler = new CreateAppointmentCommandHandler(unitOfWorkMock.Object, httpContextAccessorMock.Object);
        var command = new CreateAppointmentCommand(Guid.NewGuid(), Guid.NewGuid(), DateTime.Today, "10:00", null);
        var result = await handler.Handle(command, CancellationToken.None);
        result.ToString().Should().Contain("BadRequest");
    }

    [Fact]
    public async Task Handle_ShouldReturnOk_WhenAllValid()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var serviceId = Guid.NewGuid();
        var barber = new Barber { IsActive = true, BarberServices = new List<BarberService> { new BarberService { ServiceId = serviceId } } };
        var service = new Service { Id = serviceId, IsActive = true, DurationMinutes = 30 };
        unitOfWorkMock.Setup(u => u.Barbers.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(barber);
        unitOfWorkMock.Setup(u => u.Services.GetByIdAsync(serviceId, It.IsAny<CancellationToken>())).ReturnsAsync(service);
        unitOfWorkMock.Setup(u => u.Appointments.HasConflictAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        unitOfWorkMock.Setup(u => u.Appointments.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }, "mock"));
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        var handler = new CreateAppointmentCommandHandler(unitOfWorkMock.Object, httpContextAccessorMock.Object);
        var command = new CreateAppointmentCommand(Guid.NewGuid(), serviceId, DateTime.Today, "10:00", null);
        var result = await handler.Handle(command, CancellationToken.None);
        result.ToString().Should().Contain("Ok");
    }
}
