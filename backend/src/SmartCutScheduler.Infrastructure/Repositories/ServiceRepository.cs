using Microsoft.EntityFrameworkCore;
using SmartCutScheduler.Domain.Entities;
using SmartCutScheduler.Domain.Repositories;
using SmartCutScheduler.Infrastructure.Persistence;

namespace SmartCutScheduler.Infrastructure.Repositories;

public class ServiceRepository(AppDbContext context) : IServiceRepository
{
    public async Task<Service?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Services.FindAsync([id], cancellationToken);
    }

    public async Task<IEnumerable<Service>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = context.Services.AsQueryable();

        if (!includeInactive)
            query = query.Where(s => s.IsActive);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Service service, CancellationToken cancellationToken = default)
    {
        await context.Services.AddAsync(service, cancellationToken);
    }

    public Task UpdateAsync(Service service, CancellationToken cancellationToken = default)
    {
        context.Services.Update(service);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var service = context.Services.Find(id);
        if (service != null)
            context.Services.Remove(service);
        return Task.CompletedTask;
    }
}
