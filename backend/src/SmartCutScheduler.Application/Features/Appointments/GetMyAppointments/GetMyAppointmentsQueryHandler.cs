using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using SmartCutScheduler.Domain.Repositories;

namespace SmartCutScheduler.Application.Features.Appointments.GetMyAppointments;

public class GetMyAppointmentsQueryHandler(
    IAppointmentRepository appointmentRepository,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<GetMyAppointmentsQuery, IResult>
{
    public async Task<IResult> Handle(GetMyAppointmentsQuery request, CancellationToken cancellationToken)
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx is null || ctx.User.Identity?.IsAuthenticated != true)
            return Results.Unauthorized();

        var userIdClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Results.Unauthorized();

        var appointments = await appointmentRepository.GetByUserIdAsync(userId, cancellationToken);
        
        var result = appointments
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
            .ToList();

        return Results.Ok(result);
    }
}
