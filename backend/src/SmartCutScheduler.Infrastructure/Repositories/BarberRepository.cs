using Microsoft.EntityFrameworkCore;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Repositories;
using SmartCutScheduler.Infrastructure.Persistence;

namespace SmartCutScheduler.Infrastructure.Repositories;

public class BarberRepository(AppDbContext context) : IBarberRepository
{
    public async Task<Barber?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Barbers
            .Include(b => b.BarberServices)
                .ThenInclude(bs => bs.Service)
            .Include(b => b.WorkSchedules)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Barber>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = context.Barbers
            .Include(b => b.BarberServices)
                .ThenInclude(bs => bs.Service)
            .Include(b => b.WorkSchedules)
            .AsQueryable();

        if (!includeInactive)
            query = query.Where(b => b.IsActive);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Barber barber, CancellationToken cancellationToken = default)
    {
        await context.Barbers.AddAsync(barber, cancellationToken);
    }

    public Task UpdateAsync(Barber barber, CancellationToken cancellationToken = default)
    {
        context.Barbers.Update(barber);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var barber = context.Barbers.Find(id);
        if (barber != null)
            context.Barbers.Remove(barber);
        return Task.CompletedTask;
    }
}
