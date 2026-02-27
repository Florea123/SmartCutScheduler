using System.Net.Http.Json;

namespace SmartCutScheduler.Web.Services;

public interface IBarberServiceService
{
    Task<List<AvailableServiceDto>> GetAllServicesAsync();
    Task<List<MyServiceDto>> GetMyServicesAsync();
    Task<bool> AddServiceAsync(Guid serviceId, decimal? customPrice);
    Task<bool> CreateCustomServiceAsync(string name, string? description, int durationMinutes, decimal price);
    Task<bool> RemoveServiceAsync(Guid serviceId);
}

public class BarberServiceService : IBarberServiceService
{
    private readonly IApiService _api;

    public BarberServiceService(IApiService api)
    {
        _api = api;
    }

    public async Task<List<AvailableServiceDto>> GetAllServicesAsync()
    {
        var response = await _api.GetAsync("/api/services");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<AvailableServiceDto>>() ?? new();
        }
        return new();
    }

    public async Task<List<MyServiceDto>> GetMyServicesAsync()
    {
        var response = await _api.GetAsync("/api/barbers/me/services");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<MyServiceDto>>() ?? new();
        }
        return new();
    }

    public async Task<bool> AddServiceAsync(Guid serviceId, decimal? customPrice)
    {
        var response = await _api.PostAsync("/api/barbers/me/services", new
        {
            serviceId,
            customPrice
        });
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> CreateCustomServiceAsync(string name, string? description, int durationMinutes, decimal price)
    {
        var response = await _api.PostAsync("/api/barbers/me/services/custom", new
        {
            name,
            description,
            durationMinutes,
            price
        });
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveServiceAsync(Guid serviceId)
    {
        var response = await _api.DeleteAsync($"/api/barbers/me/services/{serviceId}");
        return response.IsSuccessStatusCode;
    }
}

public class AvailableServiceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; }
}

public class MyServiceDto
{
    public Guid ServiceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public decimal BasePrice { get; set; }
    public decimal? CustomPrice { get; set; }
    public decimal FinalPrice { get; set; }
}
