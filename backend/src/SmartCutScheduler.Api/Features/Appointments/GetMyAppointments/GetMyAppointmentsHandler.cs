using System.Security.Claims;
using SmartCutScheduler.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace SmartCutScheduler.Api.Features.Appointments.GetMyAppointments;

public class GetMyAppointmentsHandler(
    AppDbContext db,
    IHttpContextAccessor http
) : IRequestHandler<GetMyAppointmentsQuery, IResult>
{
    public async Task<IResult> Handle(GetMyAppointmentsQuery request, CancellationToken ct)
    {
        var ctx = http.HttpContext;
        if (ctx is null || ctx.User.Identity?.IsAuthenticated != true)
            return Results.Unauthorized();

        var userIdClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Results.Unauthorized();

        var appointments = await db.Appointments
            .Where(a => a.UserId == userId)
            .Include(a => a.Barber)
            .Include(a => a.Service)
            .OrderByDescending(a => a.AppointmentDate)
            .ThenByDescending(a => a.StartTime)
            .Select(a => new
            {
                a.Id,
                a.AppointmentDate,
                a.StartTime,
                a.EndTime,
                a.Status,
                a.Notes,
                Barber = new { a.Barber.Id, a.Barber.Name, a.Barber.PhotoUrl },
                Service = new { a.Service.Id, a.Service.Name, a.Service.DurationMinutes, a.Service.BasePrice }
            })
            .ToListAsync(ct);

        return Results.Ok(appointments);
    }
}
