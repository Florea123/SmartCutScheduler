using SmartCutScheduler.Api.Enums;

namespace SmartCutScheduler.Api.Domain;

public class Appointment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
    
    public Guid BarberId { get; set; }
    public Barber Barber { get; set; } = default!;
    
    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = default!;
    
    public DateTime AppointmentDate { get; set; } // Data programării
    public TimeSpan StartTime { get; set; }       // Ora de început (ex: 10:00)
    public TimeSpan EndTime { get; set; }         // Ora de sfârșit (ex: 11:00)
    
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    
    public string? Notes { get; set; } // Notițe cliente
    
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
