using Blazored.LocalStorage;
using SmartCutScheduler.Web.Models;

namespace SmartCutScheduler.Web.Services;

public class NotificationService
{
    private const string StorageKey = "ai_notifications";
    private const string ReminderKey = "ai_last_reminder_date";
    private readonly ILocalStorageService _localStorage;

    public event Action? OnChange;

    public NotificationService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task<List<AiNotification>> GetAllAsync()
    {
        return await _localStorage.GetItemAsync<List<AiNotification>>(StorageKey) ?? new();
    }

    public async Task SaveAsync(List<AiNotification> notifications)
    {
        await _localStorage.SetItemAsync(StorageKey, notifications);
        OnChange?.Invoke();
    }

    public async Task<int> GetUnreadCountAsync()
    {
        var all = await GetAllAsync();
        return all.Count(n => !n.IsRead);
    }

    public async Task AddAsync(AiNotification notification)
    {
        var all = await GetAllAsync();
        all.Insert(0, notification);
        if (all.Count > 20)
            all = all.Take(20).ToList();
        await _localStorage.SetItemAsync(StorageKey, all);
        OnChange?.Invoke();
    }

    public async Task MarkAllReadAsync()
    {
        var all = await GetAllAsync();
        foreach (var n in all)
            n.IsRead = true;
        await _localStorage.SetItemAsync(StorageKey, all);
        OnChange?.Invoke();
    }

    public async Task ClearAllAsync()
    {
        await _localStorage.RemoveItemAsync(StorageKey);
        OnChange?.Invoke();
    }

    public async Task<DateTime?> GetLastReminderDateAsync()
    {
        var raw = await _localStorage.GetItemAsync<string>(ReminderKey);
        return DateTime.TryParse(raw, out var dt) ? dt : null;
    }

    public async Task SetLastReminderDateAsync(DateTime date)
    {
        await _localStorage.SetItemAsync(ReminderKey, date.ToString("yyyy-MM-dd"));
    }
}
