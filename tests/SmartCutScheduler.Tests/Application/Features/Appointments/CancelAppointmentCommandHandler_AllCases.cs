using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using SmartCutScheduler.Application.Features.Appointments.CancelAppointment;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Enums;
using SmartCutScheduler.Domain.Repositories;
using Xunit;

public class CancelAppointmentCommandHandler_AllCases
{
    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        var appointmentRepoMock = new Mock<IAppointmentRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        var handler = new CancelAppointmentCommandHandler(appointmentRepoMock.Object, unitOfWorkMock.Object, httpContextAccessorMock.Object);
        var command = new CancelAppointmentCommand(Guid.NewGuid());
        var result = await handler.Handle(command, CancellationToken.None);
        result.ToString().Should().Contain("Unauthorized");
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenAppointmentNotFoundOrNotOwned()
    {
        var appointmentRepoMock = new Mock<IAppointmentRepository>();
        appointmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Appointment?)null);
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var httpContext = new DefaultHttpContext();
        var userId = Guid.NewGuid();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "mock"));
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        var handler = new CancelAppointmentCommandHandler(appointmentRepoMock.Object, unitOfWorkMock.Object, httpContextAccessorMock.Object);
        var command = new CancelAppointmentCommand(Guid.NewGuid());
        var result = await handler.Handle(command, CancellationToken.None);
        result.ToString().Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenAlreadyCancelled()
    {
        var appointment = new Appointment { UserId = Guid.NewGuid(), Status = AppointmentStatus.Cancelled };
        var appointmentRepoMock = new Mock<IAppointmentRepository>();
        appointmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(appointment);
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, appointment.UserId.ToString()) }, "mock"));
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        var handler = new CancelAppointmentCommandHandler(appointmentRepoMock.Object, unitOfWorkMock.Object, httpContextAccessorMock.Object);
        var command = new CancelAppointmentCommand(Guid.NewGuid());
        var result = await handler.Handle(command, CancellationToken.None);
        result.ToString().Should().Contain("BadRequest");
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenCompleted()
    {
        var appointment = new Appointment { UserId = Guid.NewGuid(), Status = AppointmentStatus.Completed };
        var appointmentRepoMock = new Mock<IAppointmentRepository>();
        appointmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(appointment);
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, appointment.UserId.ToString()) }, "mock"));
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        var handler = new CancelAppointmentCommandHandler(appointmentRepoMock.Object, unitOfWorkMock.Object, httpContextAccessorMock.Object);
        var command = new CancelAppointmentCommand(Guid.NewGuid());
        var result = await handler.Handle(command, CancellationToken.None);
        result.ToString().Should().Contain("BadRequest");
    }

    [Fact]
    public async Task Handle_ShouldReturnOk_WhenValid()
    {
        var appointment = new Appointment { UserId = Guid.NewGuid(), Status = AppointmentStatus.Confirmed };
        var appointmentRepoMock = new Mock<IAppointmentRepository>();
        appointmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(appointment);
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, appointment.UserId.ToString()) }, "mock"));
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        var handler = new CancelAppointmentCommandHandler(appointmentRepoMock.Object, unitOfWorkMock.Object, httpContextAccessorMock.Object);
        var command = new CancelAppointmentCommand(Guid.NewGuid());
        var result = await handler.Handle(command, CancellationToken.None);
        result.ToString().Should().Contain("Ok");
    }
}
