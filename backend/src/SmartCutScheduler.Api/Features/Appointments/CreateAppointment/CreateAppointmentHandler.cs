using System.Security.Claims;
using SmartCutScheduler.Api.Data;
using SmartCutScheduler.Api.Domain;
using SmartCutScheduler.Api.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace SmartCutScheduler.Api.Features.Appointments.CreateAppointment;

public class CreateAppointmentHandler(
    AppDbContext db,
    IHttpContextAccessor http
) : IRequestHandler<CreateAppointmentCommand, IResult>
{
    public async Task<IResult> Handle(CreateAppointmentCommand request, CancellationToken ct)
    {
        var ctx = http.HttpContext;
        if (ctx is null || ctx.User.Identity?.IsAuthenticated != true)
            return Results.Unauthorized();

        var userIdClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Results.Unauthorized();

        // Verify barber exists and is active
        var barber = await db.Barbers
            .FirstOrDefaultAsync(b => b.Id == request.BarberId && b.IsActive, ct);
        if (barber is null)
            return Results.NotFound("Barber not found.");

        // Verify service exists and is active
        var service = await db.Services
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId && s.IsActive, ct);
        if (service is null)
            return Results.NotFound("Service not found.");

        // Verify barber offers this service
        var barberService = await db.BarberServices
            .AnyAsync(bs => bs.BarberId == request.BarberId && bs.ServiceId == request.ServiceId, ct);
        if (!barberService)
            return Results.BadRequest("Barber does not offer this service.");

        // Parse time
        if (!TimeSpan.TryParse(request.StartTime, out var startTime))
            return Results.BadRequest("Invalid start time format.");

        var endTime = startTime.Add(TimeSpan.FromMinutes(service.DurationMinutes));

        // Check if slot is available
        var hasConflict = await db.Appointments
            .AnyAsync(a => a.BarberId == request.BarberId &&
                          a.AppointmentDate.Date == request.AppointmentDate.Date &&
                          a.Status != AppointmentStatus.Cancelled &&
                          ((startTime >= a.StartTime && startTime < a.EndTime) ||
                           (endTime > a.StartTime && endTime <= a.EndTime) ||
                           (startTime <= a.StartTime && endTime >= a.EndTime)), ct);

        if (hasConflict)
            return Results.BadRequest("This time slot is not available.");

        var appointment = new Appointment
        {
            UserId = userId,
            BarberId = request.BarberId,
            ServiceId = request.ServiceId,
            AppointmentDate = request.AppointmentDate.Date,
            StartTime = startTime,
            EndTime = endTime,
            Notes = request.Notes,
            Status = AppointmentStatus.Confirmed
        };

        db.Appointments.Add(appointment);
        await db.SaveChangesAsync(ct);

        return Results.Ok(new
        {
            appointment.Id,
            appointment.BarberId,
            appointment.ServiceId,
            appointment.AppointmentDate,
            appointment.StartTime,
            appointment.EndTime,
            appointment.Status
        });
    }
}
