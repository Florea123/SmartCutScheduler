namespace SmartCutScheduler.Web.Models;

// ── Notification stored in localStorage ─────────────────────────────────────

public class AiNotification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string BarberName { get; set; } = string.Empty;
    public string BarberId { get; set; } = string.Empty;
    public string SlotDate { get; set; } = string.Empty;
    public string SlotTime { get; set; } = string.Empty;
    public double Score { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsRead { get; set; }
}

// ── AI Service HTTP models ────────────────────────────────────────────────────

public class AiSlotInfo
{
    public string barberId { get; set; } = string.Empty;
    public string barberName { get; set; } = string.Empty;
    public double? barberRating { get; set; }
    public string serviceId { get; set; } = string.Empty;
    public string date { get; set; } = string.Empty;
    public string startTime { get; set; } = string.Empty;
    public string endTime { get; set; } = string.Empty;
}

public class AiRecommendRequest
{
    public string userId { get; set; } = string.Empty;
    public List<AiSlotInfo> availableSlots { get; set; } = new();
    public string? city { get; set; }
    public bool includeCalendar { get; set; }
    public string? authToken { get; set; }
    public string? lastHaircutDate { get; set; }
}

public class AiRecommendedSlot
{
    public string date { get; set; } = string.Empty;
    public string time { get; set; } = string.Empty;
    public string barber { get; set; } = string.Empty;
    public string barberId { get; set; } = string.Empty;
    public string serviceId { get; set; } = string.Empty;
    public string endTime { get; set; } = string.Empty;
}

public class AiScoreBreakdown
{
    public double barber_rating { get; set; }
    public double preferred_time_match { get; set; }
    public double weather_score { get; set; }
    public double calendar_availability { get; set; }
    public double haircut_urgency { get; set; }
    public double total { get; set; }
}

public class AiRecommendResponse
{
    public AiRecommendedSlot recommended_slot { get; set; } = new();
    public string reason { get; set; } = string.Empty;
    public AiScoreBreakdown score_breakdown { get; set; } = new();
}
