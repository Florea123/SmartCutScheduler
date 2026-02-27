using MediatR;
using Microsoft.AspNetCore.Http;
using SmartCutScheduler.Domain.Enums;
using SmartCutScheduler.Domain.Repositories;

namespace SmartCutScheduler.Application.Features.Availability.GetDaySlots;

public class GetDaySlotsQueryHandler(
    IBarberRepository barberRepository,
    IAppointmentRepository appointmentRepository
) : IRequestHandler<GetDaySlotsQuery, IResult>
{
    public async Task<IResult> Handle(GetDaySlotsQuery request, CancellationToken cancellationToken)
    {
        var barber = await barberRepository.GetByIdAsync(request.BarberId, cancellationToken);
        if (barber is null || !barber.IsActive)
            return Results.NotFound("Barber not found.");

        // Check if it's a working day (Monday to Friday)
        var dayOfWeek = (DayOfWeekEnum)((int)request.Date.DayOfWeek == 0 ? 7 : (int)request.Date.DayOfWeek);
        if ((int)dayOfWeek < 1 || (int)dayOfWeek > 5)
        {
            return Results.Ok(new { date = request.Date.ToString("yyyy-MM-dd"), slots = new List<object>() });
        }

        // Get existing appointments for this barber on this date
        // Convert to UTC to avoid PostgreSQL UTC issues
        var localDate = request.Date.Date;
        var utcDate = new DateTime(localDate.Year, localDate.Month, localDate.Day, 0, 0, 0, DateTimeKind.Utc);
        var appointments = await appointmentRepository.GetByBarberIdAsync(request.BarberId, utcDate, cancellationToken);
        var appointmentsList = appointments
            .Where(a => a.Status != AppointmentStatus.Cancelled)
            .ToList();

        // Generate all 30-minute slots from 08:00 to 18:00
        var slots = new List<object>();
        var startTime = new TimeSpan(8, 0, 0);
        var endTime = new TimeSpan(18, 0, 0);
        var slotDuration = TimeSpan.FromMinutes(30);

        var currentTime = startTime;
        while (currentTime < endTime)
        {
            var slotStart = utcDate.Add(currentTime);
            var slotEnd = currentTime.Add(slotDuration);

            // Check if this slot overlaps with any existing appointment
            var isBooked = appointmentsList.Any(a =>
                (currentTime >= a.StartTime && currentTime < a.EndTime) ||
                (slotEnd > a.StartTime && slotEnd <= a.EndTime) ||
                (currentTime <= a.StartTime && slotEnd >= a.EndTime));

            slots.Add(new
            {
                time = slotStart.ToString("yyyy-MM-ddTHH:mm:ss"),
                displayTime = currentTime.ToString(@"hh\:mm"),
                isBooked
            });

            currentTime = currentTime.Add(slotDuration);
        }

        return Results.Ok(new { date = request.Date.ToString("yyyy-MM-dd"), slots });
    }
}
