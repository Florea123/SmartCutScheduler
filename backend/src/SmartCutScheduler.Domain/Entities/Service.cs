namespace SmartCutScheduler.Domain.Entities;

public class Service
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public decimal BasePrice { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<BarberService> BarberServices { get; set; } = new List<BarberService>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
