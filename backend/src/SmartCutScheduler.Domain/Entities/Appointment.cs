using SmartCutScheduler.Domain.Enums;

namespace SmartCutScheduler.Domain.Entities;

public class Appointment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
    
    public Guid BarberId { get; set; }
    public Barber Barber { get; set; } = default!;
    
    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = default!;
    
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    
    public string? Notes { get; set; }
    
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
