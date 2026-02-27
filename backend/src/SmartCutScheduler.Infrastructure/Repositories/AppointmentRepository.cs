using Microsoft.EntityFrameworkCore;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Enums;
using SmartCutScheduler.Domain.Repositories;
using SmartCutScheduler.Infrastructure.Persistence;

namespace SmartCutScheduler.Infrastructure.Repositories;

public class AppointmentRepository(AppDbContext context) : IAppointmentRepository
{
    public async Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Appointments
            .Include(a => a.User)
            .Include(a => a.Barber)
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Appointment>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.Appointments
            .Include(a => a.Barber)
            .Include(a => a.Service)
            .Where(a => a.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Appointment>> GetByBarberIdAsync(Guid barberId, DateTime? date = null, CancellationToken cancellationToken = default)
    {
        var query = context.Appointments
            .Include(a => a.User)
            .Include(a => a.Service)
            .Where(a => a.BarberId == barberId);

        if (date.HasValue)
        {
            // Don't use .Date since it generates Kind.Unspecified; instead use start/end bounds
            var dateValue = date.Value;
            query = query.Where(a => a.AppointmentDate >= dateValue && a.AppointmentDate < dateValue.AddDays(1));
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Appointment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.Appointments
            .Include(a => a.User)
            .Include(a => a.Barber)
            .Include(a => a.Service)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        await context.Appointments.AddAsync(appointment, cancellationToken);
    }

    public Task UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        context.Appointments.Update(appointment);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var appointment = context.Appointments.Find(id);
        if (appointment != null)
            context.Appointments.Remove(appointment);
        return Task.CompletedTask;
    }

    public async Task<bool> HasConflictAsync(
        Guid barberId,
        DateTime date,
        TimeSpan startTime,
        TimeSpan endTime,
        Guid? excludeAppointmentId = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Appointments
            .Where(a => a.BarberId == barberId &&
                       a.AppointmentDate.Date == date.Date &&
                       a.Status != AppointmentStatus.Cancelled &&
                       ((startTime >= a.StartTime && startTime < a.EndTime) ||
                        (endTime > a.StartTime && endTime <= a.EndTime) ||
                        (startTime <= a.StartTime && endTime >= a.EndTime)));

        if (excludeAppointmentId.HasValue)
            query = query.Where(a => a.Id != excludeAppointmentId.Value);

        return await query.AnyAsync(cancellationToken);
    }
}
