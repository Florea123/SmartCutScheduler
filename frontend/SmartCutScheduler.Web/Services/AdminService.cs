using System.Net.Http.Json;
using SmartCutScheduler.Web.Models;

namespace SmartCutScheduler.Web.Services;

public interface IAdminService
{
    Task<List<UserDto>> GetAllUsersAsync();
    Task<bool> DeleteUserAsync(Guid id);
    Task<bool> DeleteBarberAsync(Guid id);
}

public class AdminService : IAdminService
{
    private readonly IApiService _api;

    public AdminService(IApiService api)
    {
        _api = api;
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var response = await _api.GetAsync("/api/users");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<UserDto>>() ?? new();
        }
        return new();
    }

    public async Task<bool> DeleteUserAsync(Guid id)
    {
        var response = await _api.DeleteAsync($"/api/users/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteBarberAsync(Guid id)
    {
        var response = await _api.DeleteAsync($"/api/barbers/{id}");
        return response.IsSuccessStatusCode;
    }
}
