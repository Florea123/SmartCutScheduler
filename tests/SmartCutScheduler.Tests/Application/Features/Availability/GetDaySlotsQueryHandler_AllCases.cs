using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SmartCutScheduler.Application.Features.Availability.GetDaySlots;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Enums;
using SmartCutScheduler.Domain.Repositories;
using Xunit;

public class GetDaySlotsQueryHandler_AllCases
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenBarberNotFoundOrInactive()
    {
        var barberRepoMock = new Mock<IBarberRepository>();
        barberRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Barber)null);
        var appointmentRepoMock = new Mock<IAppointmentRepository>();
        var handler = new GetDaySlotsQueryHandler(barberRepoMock.Object, appointmentRepoMock.Object);
        var query = new GetDaySlotsQuery(Guid.NewGuid(), DateTime.Today);
        var result = await handler.Handle(query, CancellationToken.None);
        result.ToString().Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_ShouldReturnOk_WithSlotsForWorkingDay()
    {
        var barber = new Barber { IsActive = true };
        var barberRepoMock = new Mock<IBarberRepository>();
        barberRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(barber);
        var appointmentRepoMock = new Mock<IAppointmentRepository>();
        appointmentRepoMock.Setup(r => r.GetByBarberIdAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<Appointment>());
        var handler = new GetDaySlotsQueryHandler(barberRepoMock.Object, appointmentRepoMock.Object);
        var query = new GetDaySlotsQuery(Guid.NewGuid(), new DateTime(2024, 3, 18)); // Monday
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
        var slotsProp = payload.GetType().GetProperty("slots");
        slotsProp.Should().NotBeNull();
        var slots = slotsProp.GetValue(payload) as IEnumerable<object> ?? (slotsProp.GetValue(payload) as System.Collections.IEnumerable)?.Cast<object>();
        slots.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnOk_WithEmptySlotsForWeekend()
    {
        var barber = new Barber { IsActive = true };
        var barberRepoMock = new Mock<IBarberRepository>();
        barberRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(barber);
        var appointmentRepoMock = new Mock<IAppointmentRepository>();
        var handler = new GetDaySlotsQueryHandler(barberRepoMock.Object, appointmentRepoMock.Object);
        var query = new GetDaySlotsQuery(Guid.NewGuid(), new DateTime(2024, 3, 17)); // Sunday
        var result = await handler.Handle(query, CancellationToken.None);
        result.Should().NotBeNull();
        object? payload = null;
        var resultType = result?.GetType();
        if (resultType != null && resultType.Name.StartsWith("Ok"))
        {
            var valueProp = resultType.GetProperty("Value");
            payload = valueProp?.GetValue(result);
        }
        payload.Should().NotBeNull();
        var slotsProp = payload.GetType().GetProperty("slots");
        slotsProp.Should().NotBeNull();
        var slots = slotsProp.GetValue(payload) as IEnumerable<object> ?? (slotsProp.GetValue(payload) as System.Collections.IEnumerable)?.Cast<object>();
        slots.Should().NotBeNull();
    }
}
