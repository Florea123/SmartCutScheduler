using SmartCutScheduler.Domain.Enums;

namespace SmartCutScheduler.Domain.Entities;

public class WorkSchedule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BarberId { get; set; }
    public Barber Barber { get; set; } = default!;
    
    public DayOfWeekEnum DayOfWeek { get; set; }
    
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    
    public bool IsWorkingDay { get; set; } = true;
    
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
