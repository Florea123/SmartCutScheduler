namespace SmartCutScheduler.Api.Domain;

// Relație many-to-many între Barber și Service
// Un frizer poate oferi mai multe servicii, un serviciu poate fi oferit de mai mulți frizeri
public class BarberService
{
    public Guid BarberId { get; set; }
    public Barber Barber { get; set; } = default!;
    
    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = default!;
    
    // Preț customizat pentru acest frizer (opțional, dacă diferă de BasePrice)
    public decimal? CustomPrice { get; set; }
}
