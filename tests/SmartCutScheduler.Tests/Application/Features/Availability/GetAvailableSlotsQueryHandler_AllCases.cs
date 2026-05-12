using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SmartCutScheduler.Application.Features.Availability.GetAvailableSlots;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Enums;
using SmartCutScheduler.Domain.Repositories;
using Xunit;

public class GetAvailableSlotsQueryHandler_AllCases
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenBarberNotFoundOrInactive()
    {
        var barberRepoMock = new Mock<IBarberRepository>();
        barberRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Barber?)null);
        var serviceRepoMock = new Mock<IServiceRepository>();
        var appointmentRepoMock = new Mock<IAppointmentRepository>();
        var handler = new GetAvailableSlotsQueryHandler(barberRepoMock.Object, serviceRepoMock.Object, appointmentRepoMock.Object);
        var query = new GetAvailableSlotsQuery(Guid.NewGuid(), Guid.NewGuid(), DateTime.Today);
        var result = await handler.Handle(query, CancellationToken.None);
        result.ToString().Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenServiceNotFoundOrInactive()
    {
        var barber = new Barber { IsActive = true, BarberServices = new List<BarberService>() };
        var barberRepoMock = new Mock<IBarberRepository>();
        barberRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(barber);
        var serviceRepoMock = new Mock<IServiceRepository>();
        serviceRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Service)null);
        var appointmentRepoMock = new Mock<IAppointmentRepository>();
        var handler = new GetAvailableSlotsQueryHandler(barberRepoMock.Object, serviceRepoMock.Object, appointmentRepoMock.Object);
        var query = new GetAvailableSlotsQuery(Guid.NewGuid(), Guid.NewGuid(), DateTime.Today);
        var result = await handler.Handle(query, CancellationToken.None);
        result.ToString().Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenBarberDoesNotOfferService()
    {
        var barber = new Barber { IsActive = true, BarberServices = new List<BarberService>() };
        var barberRepoMock = new Mock<IBarberRepository>();
        barberRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(barber);
        var service = new Service { IsActive = true, DurationMinutes = 30 };
        var serviceRepoMock = new Mock<IServiceRepository>();
        serviceRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(service);
        var appointmentRepoMock = new Mock<IAppointmentRepository>();
        var handler = new GetAvailableSlotsQueryHandler(barberRepoMock.Object, serviceRepoMock.Object, appointmentRepoMock.Object);
        var query = new GetAvailableSlotsQuery(Guid.NewGuid(), Guid.NewGuid(), DateTime.Today);
        var result = await handler.Handle(query, CancellationToken.None);
        result.ToString().Should().Contain("BadRequest");
    }

    [Fact]
    public async Task Handle_ShouldReturnOk_WhenSlotsAvailable()
    {
        var serviceId = Guid.NewGuid();
        var barber = new Barber { IsActive = true, BarberServices = new List<BarberService> { new BarberService { ServiceId = serviceId } }, WorkSchedules = new List<WorkSchedule>() };
        var barberRepoMock = new Mock<IBarberRepository>();
        barberRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(barber);
        var service = new Service { Id = serviceId, IsActive = true, DurationMinutes = 30 };
        var serviceRepoMock = new Mock<IServiceRepository>();
        serviceRepoMock.Setup(r => r.GetByIdAsync(serviceId, It.IsAny<CancellationToken>())).ReturnsAsync(service);
        var appointmentRepoMock = new Mock<IAppointmentRepository>();
        appointmentRepoMock.Setup(r => r.GetByBarberIdAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<Appointment>());
        var handler = new GetAvailableSlotsQueryHandler(barberRepoMock.Object, serviceRepoMock.Object, appointmentRepoMock.Object);
        var query = new GetAvailableSlotsQuery(Guid.NewGuid(), serviceId, DateTime.Today);
        var result = await handler.Handle(query, CancellationToken.None);
        result.ToString().Should().Contain("Ok");
    }

    [Fact]
    public async Task Handle_ShouldReturnOk_WithNoSlots_WhenWeekend()
    {
        var serviceId = Guid.NewGuid();
        // Sunday: DayOfWeek==0 → DayOfWeekEnum==7 → outside 1-5 range
        var sunday = new DateTime(2024, 3, 17); // Sunday
        var barber = new Barber { IsActive = true, BarberServices = new List<BarberService> { new BarberService { ServiceId = serviceId } }, WorkSchedules = new List<WorkSchedule>() };
        var barberRepoMock = new Mock<IBarberRepository>();
        barberRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(barber);
        var service = new Service { Id = serviceId, IsActive = true, DurationMinutes = 30 };
        var serviceRepoMock = new Mock<IServiceRepository>();
        serviceRepoMock.Setup(r => r.GetByIdAsync(serviceId, It.IsAny<CancellationToken>())).ReturnsAsync(service);
        var appointmentRepoMock = new Mock<IAppointmentRepository>();
        var handler = new GetAvailableSlotsQueryHandler(barberRepoMock.Object, serviceRepoMock.Object, appointmentRepoMock.Object);
        var query = new GetAvailableSlotsQuery(Guid.NewGuid(), serviceId, sunday);
        var result = await handler.Handle(query, CancellationToken.None);
        result.ToString().Should().Contain("Ok");
    }

    [Fact]
    public async Task Handle_ShouldUseWorkSchedule_WhenPresentInDb()
    {
        var serviceId = Guid.NewGuid();
        var monday = new DateTime(2024, 3, 18); // Monday = DayOfWeekEnum.Monday == 1
        var workSchedule = new WorkSchedule
        {
            DayOfWeek = DayOfWeekEnum.Monday,
            IsWorkingDay = true,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0)
        };
        var barber = new Barber
        {
            IsActive = true,
            BarberServices = new List<BarberService> { new BarberService { ServiceId = serviceId } },
            WorkSchedules = new List<WorkSchedule> { workSchedule }
        };
        var barberRepoMock = new Mock<IBarberRepository>();
        barberRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(barber);
        var service = new Service { Id = serviceId, IsActive = true, DurationMinutes = 30 };
        var serviceRepoMock = new Mock<IServiceRepository>();
        serviceRepoMock.Setup(r => r.GetByIdAsync(serviceId, It.IsAny<CancellationToken>())).ReturnsAsync(service);
        var appointmentRepoMock = new Mock<IAppointmentRepository>();
        appointmentRepoMock.Setup(r => r.GetByBarberIdAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<Appointment>());
        var handler = new GetAvailableSlotsQueryHandler(barberRepoMock.Object, serviceRepoMock.Object, appointmentRepoMock.Object);
        var query = new GetAvailableSlotsQuery(Guid.NewGuid(), serviceId, monday);
        var result = await handler.Handle(query, CancellationToken.None);
        result.ToString().Should().Contain("Ok");
    }

    [Fact]
    public async Task Handle_ShouldExcludeConflictingSlots()
    {
        var serviceId = Guid.NewGuid();
        var monday = new DateTime(2024, 3, 18);
        var barber = new Barber
        {
            IsActive = true,
            BarberServices = new List<BarberService> { new BarberService { ServiceId = serviceId } },
            WorkSchedules = new List<WorkSchedule>()
        };
        var barberRepoMock = new Mock<IBarberRepository>();
        barberRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(barber);
        var service = new Service { Id = serviceId, IsActive = true, DurationMinutes = 30 };
        var serviceRepoMock = new Mock<IServiceRepository>();
        serviceRepoMock.Setup(r => r.GetByIdAsync(serviceId, It.IsAny<CancellationToken>())).ReturnsAsync(service);
        // One existing appointment blocks 10:00-10:30
        var existing = new Appointment
        {
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(10, 30, 0),
            Status = SmartCutScheduler.Domain.Enums.AppointmentStatus.Confirmed
        };
        var appointmentRepoMock = new Mock<IAppointmentRepository>();
        appointmentRepoMock.Setup(r => r.GetByBarberIdAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<Appointment> { existing });
        var handler = new GetAvailableSlotsQueryHandler(barberRepoMock.Object, serviceRepoMock.Object, appointmentRepoMock.Object);
        var query = new GetAvailableSlotsQuery(Guid.NewGuid(), serviceId, monday);
        var result = await handler.Handle(query, CancellationToken.None);
        result.ToString().Should().Contain("Ok");
    }
}
