using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using SmartCutScheduler.Application.Features.Appointments.GetMyAppointments;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Enums;
using SmartCutScheduler.Domain.Repositories;
using Xunit;

public class GetMyAppointmentsQueryHandler_AllCases
{
    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        var appointmentRepoMock = new Mock<IAppointmentRepository>();
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext)null);
        var handler = new GetMyAppointmentsQueryHandler(appointmentRepoMock.Object, httpContextAccessorMock.Object);
        var query = new GetMyAppointmentsQuery();
        var result = await handler.Handle(query, CancellationToken.None);
        result.ToString().Should().Contain("Unauthorized");
    }

    [Fact]
    public async Task Handle_ShouldReturnOk_WithAppointmentsList()
    {
        var userId = Guid.NewGuid();
        var appointments = new List<Appointment>
        {
            new Appointment { Id = Guid.NewGuid(), UserId = userId, AppointmentDate = DateTime.Today, StartTime = new TimeSpan(10,0,0), Status = AppointmentStatus.Confirmed, User = new User { Name = "User" }, Barber = new Barber { Name = "Barber" }, Service = new Service { Name = "Service" } }
        };
        var appointmentRepoMock = new Mock<IAppointmentRepository>();
        appointmentRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(appointments);
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "mock"));
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        var handler = new GetMyAppointmentsQueryHandler(appointmentRepoMock.Object, httpContextAccessorMock.Object);
        var query = new GetMyAppointmentsQuery();
        var result = await handler.Handle(query, CancellationToken.None);
        result.Should().NotBeNull();
        object? payload = null;
        // Try to extract Value property from Ok<T> using reflection if pattern matching fails
        var resultType = result?.GetType();
        if (resultType != null && resultType.Name.StartsWith("Ok"))
        {
            var valueProp = resultType.GetProperty("Value");
            payload = valueProp?.GetValue(result);
        }
        payload.Should().NotBeNull();
        var enumerable = payload as IEnumerable<object> ?? (payload as System.Collections.IEnumerable)?.Cast<object>();
        enumerable.Should().NotBeNull();
        var first = enumerable.FirstOrDefault();
        first.Should().NotBeNull();
        var userNameProp = first.GetType().GetProperty("UserName");
        userNameProp.Should().NotBeNull();
        userNameProp.GetValue(first).Should().Be("User");
    }
}
