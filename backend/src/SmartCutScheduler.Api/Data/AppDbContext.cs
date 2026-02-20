using Microsoft.EntityFrameworkCore;
using SmartCutScheduler.Api.Domain;
using SmartCutScheduler.Api.Enums;
using Microsoft.AspNetCore.Identity;

namespace SmartCutScheduler.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Barber> Barbers => Set<Barber>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<BarberService> BarberServices => Set<BarberService>();
    public DbSet<WorkSchedule> WorkSchedules => Set<WorkSchedule>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    public async Task EnsureSeedAdminAsync(
        string name,
        string email,
        string password,
        PasswordHasher<User> hasher,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        if (await Users.AnyAsync(u => u.Email == email, cancellationToken))
            return;

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            Role = UserRole.Admin,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        admin.PasswordHash = hasher.HashPassword(admin, password);
        await Users.AddAsync(admin, cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // BarberService many-to-many configuration
        modelBuilder.Entity<BarberService>()
            .HasKey(bs => new { bs.BarberId, bs.ServiceId });

        modelBuilder.Entity<BarberService>()
            .HasOne(bs => bs.Barber)
            .WithMany(b => b.BarberServices)
            .HasForeignKey(bs => bs.BarberId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BarberService>()
            .HasOne(bs => bs.Service)
            .WithMany(s => s.BarberServices)
            .HasForeignKey(bs => bs.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        // WorkSchedule configuration
        modelBuilder.Entity<WorkSchedule>()
            .HasIndex(ws => new { ws.BarberId, ws.DayOfWeek })
            .IsUnique();

        // Appointment configuration
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.User)
            .WithMany(u => u.Appointments)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Barber)
            .WithMany(b => b.Appointments)
            .HasForeignKey(a => a.BarberId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Service)
            .WithMany(s => s.Appointments)
            .HasForeignKey(a => a.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var e in ChangeTracker.Entries<User>())
        {
            if (e.State == EntityState.Modified)
                e.Entity.UpdatedAtUtc = now;
        }

        foreach (var e in ChangeTracker.Entries<Barber>())
        {
            if (e.State == EntityState.Modified)
                e.Entity.UpdatedAtUtc = now;
        }

        foreach (var e in ChangeTracker.Entries<Service>())
        {
            if (e.State == EntityState.Modified)
                e.Entity.UpdatedAtUtc = now;
        }

        foreach (var e in ChangeTracker.Entries<Appointment>())
        {
            if (e.State == EntityState.Modified)
                e.Entity.UpdatedAtUtc = now;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
