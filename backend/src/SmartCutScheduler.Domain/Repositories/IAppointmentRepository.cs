using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Enums;

namespace SmartCutScheduler.Domain.Repositories;

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Appointment>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Appointment>> GetByBarberIdAsync(Guid barberId, DateTime? date = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Appointment>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasConflictAsync(Guid barberId, DateTime date, TimeSpan startTime, TimeSpan endTime, Guid? excludeAppointmentId = null, CancellationToken cancellationToken = default);
}
