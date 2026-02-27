namespace SmartCutScheduler.Web.Models;

public class AppointmentDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public Guid BarberId { get; set; }
    public string BarberName { get; set; } = string.Empty;
    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public DateTime ScheduledStartUtc { get; set; }
    public DateTime ScheduledEndUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class CreateAppointmentRequest
{
    public Guid BarberId { get; set; }
    public Guid ServiceId { get; set; }
    public DateTime ScheduledStartUtc { get; set; }
    public string? Notes { get; set; }
}

public class AvailableSlot
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsAvailable { get; set; }
}
