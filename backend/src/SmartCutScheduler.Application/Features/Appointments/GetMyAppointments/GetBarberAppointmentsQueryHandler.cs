using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using SmartCutScheduler.Domain.Repositories;

namespace SmartCutScheduler.Application.Features.Appointments.GetMyAppointments;

public class GetBarberAppointmentsQueryHandler(
    IAppointmentRepository appointmentRepository,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<GetBarberAppointmentsQuery, IResult>
{
    public async Task<IResult> Handle(GetBarberAppointmentsQuery request, CancellationToken cancellationToken)
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx is null || ctx.User.Identity?.IsAuthenticated != true)
            return Results.Unauthorized();

        var userIdClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var barberId))
            return Results.Unauthorized();

        var appointments = await appointmentRepository.GetByBarberIdAsync(barberId, null, cancellationToken);

        var result = appointments
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.StartTime)
            .Select(a => new
            {
                a.Id,
                a.UserId,
                UserName = a.User?.Name ?? string.Empty,
                a.BarberId,
                BarberName = a.Barber?.Name ?? string.Empty,
                a.ServiceId,
                ServiceName = a.Service?.Name ?? string.Empty,
                AppointmentDate = a.AppointmentDate.ToString("yyyy-MM-dd"),
                StartTime = a.StartTime.ToString("hh\\:mm"),
                EndTime = a.EndTime.ToString("hh\\:mm"),
                Status = a.Status.ToString(),
                a.Notes
            })
            .ToList();

        return Results.Ok(result);
    }
}
