namespace SmartCutScheduler.Domain.Repositories;

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IBarberRepository Barbers { get; }
    IServiceRepository Services { get; }
    IAppointmentRepository Appointments { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
