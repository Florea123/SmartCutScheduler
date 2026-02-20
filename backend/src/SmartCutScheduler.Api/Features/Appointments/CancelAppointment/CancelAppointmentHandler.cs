using System.Security.Claims;
using SmartCutScheduler.Api.Data;
using SmartCutScheduler.Api.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace SmartCutScheduler.Api.Features.Appointments.CancelAppointment;

public class CancelAppointmentHandler(
    AppDbContext db,
    IHttpContextAccessor http
) : IRequestHandler<CancelAppointmentCommand, IResult>
{
    public async Task<IResult> Handle(CancelAppointmentCommand request, CancellationToken ct)
    {
        var ctx = http.HttpContext;
        if (ctx is null || ctx.User.Identity?.IsAuthenticated != true)
            return Results.Unauthorized();

        var userIdClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Results.Unauthorized();

        var appointment = await db.Appointments
            .FirstOrDefaultAsync(a => a.Id == request.Id && a.UserId == userId, ct);

        if (appointment is null)
            return Results.NotFound("Appointment not found.");

        if (appointment.Status == AppointmentStatus.Cancelled)
            return Results.BadRequest("Appointment is already cancelled.");

        if (appointment.Status == AppointmentStatus.Completed)
            return Results.BadRequest("Cannot cancel a completed appointment.");

        appointment.Status = AppointmentStatus.Cancelled;
        await db.SaveChangesAsync(ct);

        return Results.Ok("Appointment cancelled successfully.");
    }
}
