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
using Microsoft.AspNetCore.Http.Features;

public class CreateAppointmentCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext)null);
        var handler = new CreateAppointmentCommandHandler(unitOfWorkMock.Object, httpContextAccessorMock.Object);
        var command = new CreateAppointmentCommand(Guid.NewGuid(), Guid.NewGuid(), DateTime.Today, "10:00", null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.ToString().Should().Contain("Unauthorized");
    }

    // Alte teste pentru fiecare caz: barber inexistent, service inexistent, slot ocupat, succes, input invalid etc.
}
