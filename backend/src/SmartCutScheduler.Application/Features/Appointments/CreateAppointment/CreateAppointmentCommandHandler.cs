using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Enums;
using SmartCutScheduler.Domain.Repositories;

namespace SmartCutScheduler.Application.Features.Appointments.CreateAppointment;

public class CreateAppointmentCommandHandler(
    IUnitOfWork unitOfWork,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<CreateAppointmentCommand, IResult>
{
    public async Task<IResult> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx is null || ctx.User.Identity?.IsAuthenticated != true)
            return Results.Unauthorized();

        var userIdClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Results.Unauthorized();

        // Verify barber exists and is active
        var barber = await unitOfWork.Barbers.GetByIdAsync(request.BarberId, cancellationToken);
        if (barber is null || !barber.IsActive)
            return Results.NotFound("Barber not found.");

        // Verify service exists and is active
        var service = await unitOfWork.Services.GetByIdAsync(request.ServiceId, cancellationToken);
        if (service is null || !service.IsActive)
            return Results.NotFound("Service not found.");

        // Verify barber offers this service
        var barberOffersService = barber.BarberServices
            .Any(bs => bs.ServiceId == request.ServiceId);
        if (!barberOffersService)
            return Results.BadRequest("Barber does not offer this service.");

        // Parse time
        if (!TimeSpan.TryParse(request.StartTime, out var startTime))
            return Results.BadRequest("Invalid start time format.");

        var endTime = startTime.Add(TimeSpan.FromMinutes(service.DurationMinutes));

        // Check if slot is available
        var hasConflict = await unitOfWork.Appointments.HasConflictAsync(
            request.BarberId,
            request.AppointmentDate.Date,
            startTime,
            endTime,
            null,
            cancellationToken);

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

        await unitOfWork.Appointments.AddAsync(appointment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

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
