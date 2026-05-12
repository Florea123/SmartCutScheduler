namespace SmartCutScheduler.Web.State;

/// <summary>
/// Singleton application state store.
/// Holds global UI state shared across Blazor components and pages.
/// Components subscribe to OnStateChanged and call StateHasChanged() on notification.
/// </summary>
public class AppState
{
    // ── Selected barber (e.g. picked on Home, used in AppointmentNew) ──────
    public Guid? SelectedBarberId { get; private set; }
    public string? SelectedBarberName { get; private set; }

    // ── Pre-selected slot from AI recommendation ────────────────────────────
    public AiRecommendationState? AiRecommendation { get; private set; }

    // ── Notification badge counter (incremented without re-fetching storage)
    public int UnreadNotificationCount { get; private set; }

    // ── State change event – components subscribe to trigger re-render ──────
    public event Action? OnStateChanged;

    // ── Barber selection ────────────────────────────────────────────────────
    public void SelectBarber(Guid barberId, string barberName)
    {
        SelectedBarberId = barberId;
        SelectedBarberName = barberName;
        Notify();
    }

    public void ClearSelectedBarber()
    {
        SelectedBarberId = null;
        SelectedBarberName = null;
        Notify();
    }

    // ── AI recommendation ───────────────────────────────────────────────────
    public void SetAiRecommendation(AiRecommendationState recommendation)
    {
        AiRecommendation = recommendation;
        Notify();
    }

    public void ClearAiRecommendation()
    {
        AiRecommendation = null;
        Notify();
    }

    // ── Notifications ───────────────────────────────────────────────────────
    public void SetUnreadCount(int count)
    {
        UnreadNotificationCount = count;
        Notify();
    }

    public void IncrementUnreadCount()
    {
        UnreadNotificationCount++;
        Notify();
    }

    public void ResetUnreadCount()
    {
        UnreadNotificationCount = 0;
        Notify();
    }

    // ── Internal ────────────────────────────────────────────────────────────
    private void Notify() => OnStateChanged?.Invoke();
}

/// <summary>
/// Immutable snapshot of an AI-recommended appointment slot.
/// </summary>
public sealed record AiRecommendationState(
    Guid BarberId,
    string BarberName,
    string SlotDate,
    string SlotTime,
    double Score,
    string Reason);
