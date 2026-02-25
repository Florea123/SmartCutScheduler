namespace SmartCutScheduler.Domain.Entities;

public class BarberService
{
    public Guid BarberId { get; set; }
    public Barber Barber { get; set; } = default!;
    
    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = default!;
    
    public decimal? CustomPrice { get; set; }
}
