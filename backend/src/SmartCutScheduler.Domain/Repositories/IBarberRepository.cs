using SmartCutScheduler.Domain.Entities;

namespace SmartCutScheduler.Domain.Repositories;

public interface IBarberRepository
{
    Task<Barber?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Barber>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task AddAsync(Barber barber, CancellationToken cancellationToken = default);
    Task UpdateAsync(Barber barber, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
