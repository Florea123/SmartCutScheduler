using System.Net.Http.Json;
using SmartCutScheduler.Web.Models;

namespace SmartCutScheduler.Web.Services;

public interface IBarberService
{
    Task<List<BarberDto>> GetAllBarbersAsync();
    Task<BarberDto?> GetBarberByIdAsync(Guid id);
    Task<List<ServiceDto>> GetServicesAsync();
    Task<List<WorkScheduleDto>> GetBarberWorkScheduleAsync(Guid barberId);
}

public class BarberService : IBarberService
{
    private readonly IApiService _api;

    public BarberService(IApiService api)
    {
        _api = api;
    }

    public async Task<List<BarberDto>> GetAllBarbersAsync()
    {
        var response = await _api.GetAsync("/api/barbers");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<BarberDto>>() ?? new();
        }
        return new();
    }

    public async Task<BarberDto?> GetBarberByIdAsync(Guid id)
    {
        var response = await _api.GetAsync($"/api/barbers/{id}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<BarberDto>();
        }
        return null;
    }

    public async Task<List<ServiceDto>> GetServicesAsync()
    {
        var response = await _api.GetAsync("/api/services");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<ServiceDto>>() ?? new();
        }
        return new();
    }

    public async Task<List<WorkScheduleDto>> GetBarberWorkScheduleAsync(Guid barberId)
    {
        var response = await _api.GetAsync($"/api/barbers/{barberId}/schedule");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<WorkScheduleDto>>() ?? new();
        }
        return new();
    }
}
