using System.Net.Http.Json;
using SmartCutScheduler.Web.Models;

namespace SmartCutScheduler.Web.Services;

public interface IAppointmentService
{
    Task<List<AppointmentDto>> GetMyAppointmentsAsync();
    Task<List<DateTime>> GetAvailableSlotsAsync(Guid barberId, Guid serviceId, DateTime date);
    Task<List<TimeSlotDto>> GetDaySlotsAsync(Guid barberId, DateTime date);
    Task<bool> CreateAppointmentAsync(CreateAppointmentRequest request);
    Task<bool> CancelAppointmentAsync(Guid appointmentId);
}

public class AppointmentService : IAppointmentService
{
    private readonly IApiService _api;

    public AppointmentService(IApiService api)
    {
        _api = api;
    }

    public async Task<List<AppointmentDto>> GetMyAppointmentsAsync()
    {
        var response = await _api.GetAsync("/api/appointments/my");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<AppointmentDto>>() ?? new();
        }
        return new();
    }

    public async Task<List<DateTime>> GetAvailableSlotsAsync(Guid barberId, Guid serviceId, DateTime date)
    {
        var dateStr = date.ToString("yyyy-MM-dd");
        var response = await _api.GetAsync($"/api/availability?barberId={barberId}&serviceId={serviceId}&date={dateStr}");
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<AvailabilitySlotsResponse>();
            if (result?.AvailableSlots != null)
            {
                return result.AvailableSlots.Select(s => DateTime.Parse(s)).ToList();
            }
        }
        return new();
    }

    public async Task<List<TimeSlotDto>> GetDaySlotsAsync(Guid barberId, DateTime date)
    {
        var dateStr = date.ToString("yyyy-MM-dd");
        var response = await _api.GetAsync($"/api/barbers/{barberId}/day-slots?date={dateStr}");
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<DaySlotsResponse>();
            if (result?.Slots != null)
            {
                return result.Slots;
            }
        }
        return new();
    }

    public async Task<bool> CreateAppointmentAsync(CreateAppointmentRequest request)
    {
        var response = await _api.PostAsync("/api/appointments", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> CancelAppointmentAsync(Guid appointmentId)
    {
        var response = await _api.PutAsync($"/api/appointments/{appointmentId}/cancel", new { });
        return response.IsSuccessStatusCode;
    }
}

public class AvailabilitySlotsResponse
{
    public string Date { get; set; } = string.Empty;
    public List<string> AvailableSlots { get; set; } = new();
}

public class DaySlotsResponse
{
    public string Date { get; set; } = string.Empty;
    public List<TimeSlotDto> Slots { get; set; } = new();
}

public class TimeSlotDto
{
    public string Time { get; set; } = string.Empty;
    public string DisplayTime { get; set; } = string.Empty;
    public bool IsBooked { get; set; }
}
