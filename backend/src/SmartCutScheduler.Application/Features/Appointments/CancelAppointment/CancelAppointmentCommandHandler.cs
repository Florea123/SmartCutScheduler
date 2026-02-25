using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using SmartCutScheduler.Domain.Enums;
using SmartCutScheduler.Domain.Repositories;

namespace SmartCutScheduler.Application.Features.Appointments.CancelAppointment;

public class CancelAppointmentCommandHandler(
    IAppointmentRepository appointmentRepository,
    IUnitOfWork unitOfWork,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<CancelAppointmentCommand, IResult>
{
    public async Task<IResult> Handle(CancelAppointmentCommand request, CancellationToken cancellationToken)
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx is null || ctx.User.Identity?.IsAuthenticated != true)
            return Results.Unauthorized();

        var userIdClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Results.Unauthorized();

        var appointment = await appointmentRepository.GetByIdAsync(request.Id, cancellationToken);

        if (appointment is null || appointment.UserId != userId)
            return Results.NotFound("Appointment not found.");

        if (appointment.Status == AppointmentStatus.Cancelled)
            return Results.BadRequest("Appointment is already cancelled.");

        if (appointment.Status == AppointmentStatus.Completed)
            return Results.BadRequest("Cannot cancel a completed appointment.");

        appointment.Status = AppointmentStatus.Cancelled;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Results.Ok("Appointment cancelled successfully.");
    }
}
