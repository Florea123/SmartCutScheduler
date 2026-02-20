using SmartCutScheduler.Api.Data;
using SmartCutScheduler.Api.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace SmartCutScheduler.Api.Features.Availability;

public class GetAvailableSlotsHandler(AppDbContext db) : IRequestHandler<GetAvailableSlotsQuery, IResult>
{
    public async Task<IResult> Handle(GetAvailableSlotsQuery request, CancellationToken ct)
    {
        var barber = await db.Barbers
            .Include(b => b.WorkSchedules)
            .FirstOrDefaultAsync(b => b.Id == request.BarberId && b.IsActive, ct);

        if (barber is null)
            return Results.NotFound("Barber not found.");

        var service = await db.Services
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId && s.IsActive, ct);

        if (service is null)
            return Results.NotFound("Service not found.");

        // Check if barber offers this service
        var barberService = await db.BarberServices
            .AnyAsync(bs => bs.BarberId == request.BarberId && bs.ServiceId == request.ServiceId, ct);

        if (!barberService)
            return Results.BadRequest("Barber does not offer this service.");

        // Get work schedule for the requested day
        var dayOfWeek = (DayOfWeekEnum)((int)request.Date.DayOfWeek == 0 ? 7 : (int)request.Date.DayOfWeek);
        var workSchedule = barber.WorkSchedules
            .FirstOrDefault(ws => ws.DayOfWeek == dayOfWeek && ws.IsWorkingDay);

        if (workSchedule is null)
            return Results.Ok(new { availableSlots = new List<object>(), message = "Barber is not working on this day." });

        // Get existing appointments for this barber on this date
        var appointments = await db.Appointments
            .Where(a => a.BarberId == request.BarberId &&
                       a.AppointmentDate.Date == request.Date.Date &&
                       a.Status != AppointmentStatus.Cancelled)
            .Select(a => new { a.StartTime, a.EndTime })
            .ToListAsync(ct);

        // Generate time slots
        var slots = new List<object>();
        var currentTime = workSchedule.StartTime;
        var serviceDuration = TimeSpan.FromMinutes(service.DurationMinutes);

        while (currentTime.Add(serviceDuration) <= workSchedule.EndTime)
        {
            var slotEnd = currentTime.Add(serviceDuration);
            
            // Check if this slot overlaps with any existing appointment
            var isAvailable = !appointments.Any(a =>
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
