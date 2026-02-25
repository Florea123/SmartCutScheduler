using MediatR;
using Microsoft.AspNetCore.Http;
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

        if (workSchedule is null)
            return Results.Ok(new { availableSlots = new List<object>(), message = "Barber is not working on this day." });

        // Get existing appointments for this barber on this date
        var appointments = await appointmentRepository.GetByBarberIdAsync(request.BarberId, request.Date.Date, cancellationToken);
        var appointmentsList = appointments
            .Where(a => a.Status != AppointmentStatus.Cancelled)
            .Select(a => new { a.StartTime, a.EndTime })
            .ToList();

        // Generate time slots
        var slots = new List<object>();
        var currentTime = workSchedule.StartTime;
        var serviceDuration = TimeSpan.FromMinutes(service.DurationMinutes);

        while (currentTime.Add(serviceDuration) <= workSchedule.EndTime)
        {
            var slotEnd = currentTime.Add(serviceDuration);
            
            // Check if this slot overlaps with any existing appointment
            var isAvailable = !appointmentsList.Any(a =>
                (currentTime >= a.StartTime && currentTime < a.EndTime) ||
                (slotEnd > a.StartTime && slotEnd <= a.EndTime) ||
                (currentTime <= a.StartTime && slotEnd >= a.EndTime));

            slots.Add(new
            {
                startTime = currentTime.ToString(@"hh\:mm"),
                endTime = slotEnd.ToString(@"hh\:mm"),
                isAvailable
            });

            currentTime = currentTime.Add(TimeSpan.FromMinutes(30)); // 30-minute intervals
        }

        return Results.Ok(new { date = request.Date.ToString("yyyy-MM-dd"), slots });
    }
}
