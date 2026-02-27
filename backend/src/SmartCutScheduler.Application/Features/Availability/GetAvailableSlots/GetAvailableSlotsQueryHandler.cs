using MediatR;
using Microsoft.AspNetCore.Http;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Enums;
using SmartCutScheduler.Domain.Repositories;

namespace SmartCutScheduler.Application.Features.Availability.GetAvailableSlots;

public class GetAvailableSlotsQueryHandler(
    IBarberRepository barberRepository,
    IServiceRepository serviceRepository,
    IAppointmentRepository appointmentRepository
) : IRequestHandler<GetAvailableSlotsQuery, IResult>
{
    public async Task<IResult> Handle(GetAvailableSlotsQuery request, CancellationToken cancellationToken)
    {
        var barber = await barberRepository.GetByIdAsync(request.BarberId, cancellationToken);
        if (barber is null || !barber.IsActive)
            return Results.NotFound("Barber not found.");

        var service = await serviceRepository.GetByIdAsync(request.ServiceId, cancellationToken);
        if (service is null || !service.IsActive)
            return Results.NotFound("Service not found.");

        // Check if barber offers this service
        var barberOffersService = barber.BarberServices
            .Any(bs => bs.ServiceId == request.ServiceId);
        if (!barberOffersService)
            return Results.BadRequest("Barber does not offer this service.");

        // Get work schedule for the requested day
        var dayOfWeek = (DayOfWeekEnum)((int)request.Date.DayOfWeek == 0 ? 7 : (int)request.Date.DayOfWeek);
        var workSchedule = barber.WorkSchedules
            .FirstOrDefault(ws => ws.DayOfWeek == dayOfWeek && ws.IsWorkingDay);

        // If no work schedule in DB, use default Monday-Friday 08:00-18:00
        if (workSchedule is null)
        {
            // Check if it's a working day (Monday to Friday)
            if ((int)dayOfWeek >= 1 && (int)dayOfWeek <= 5)
            {
                // Use default schedule: 08:00-18:00
                var defaultStartTime = new TimeSpan(8, 0, 0);
                var defaultEndTime = new TimeSpan(18, 0, 0);
                
                // Get existing appointments for this barber on this date
                var appointments = await appointmentRepository.GetByBarberIdAsync(request.BarberId, request.Date.Date, cancellationToken);
                var appointmentsList = appointments
                    .Where(a => a.Status != AppointmentStatus.Cancelled)
                    .ToList();

                // Generate time slots with default schedule
                return Results.Ok(GenerateSlots(defaultStartTime, defaultEndTime, service.DurationMinutes, request.Date, appointmentsList));
            }
            
            return Results.Ok(new { availableSlots = new List<object>(), message = "Barber is not working on this day." });
        }

        // Get existing appointments for this barber on this date
        var appointmentsWithSchedule = await appointmentRepository.GetByBarberIdAsync(request.BarberId, request.Date.Date, cancellationToken);
        var appointmentsListWithSchedule = appointmentsWithSchedule
            .Where(a => a.Status != AppointmentStatus.Cancelled)
            .ToList();

        // Generate time slots with work schedule from DB
        return Results.Ok(GenerateSlots(workSchedule.StartTime, workSchedule.EndTime, service.DurationMinutes, request.Date, appointmentsListWithSchedule));
    }

    private static object GenerateSlots(TimeSpan startTime, TimeSpan endTime, int serviceDuration, DateTime date, IEnumerable<Appointment> appointments)
    {
        var slots = new List<string>();
        var currentTime = startTime;
        var serviceDurationSpan = TimeSpan.FromMinutes(serviceDuration);

        while (currentTime.Add(serviceDurationSpan) <= endTime)
        {
            var slotEnd = currentTime.Add(serviceDurationSpan);
            
            // Check if this slot overlaps with any existing appointment
            var isAvailable = !appointments.Any(a =>
                (currentTime >= a.StartTime && currentTime < a.EndTime) ||
                (slotEnd > a.StartTime && slotEnd <= a.EndTime) ||
                (currentTime <= a.StartTime && slotEnd >= a.EndTime));

            if (isAvailable)
            {
                // Return full DateTime for the slot
                var slotDateTime = date.Date.Add(currentTime);
                slots.Add(slotDateTime.ToString("yyyy-MM-ddTHH:mm:ss"));
            }

            currentTime = currentTime.Add(TimeSpan.FromMinutes(30)); // 30-minute intervals
        }

        return new { date = date.ToString("yyyy-MM-dd"), availableSlots = slots };
    }
}
