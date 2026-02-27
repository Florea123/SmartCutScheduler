using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;

namespace SmartCutScheduler.Web.Services;

public interface IApiService
{
    Task<HttpResponseMessage> GetAsync(string endpoint);
    Task<HttpResponseMessage> PostAsync<T>(string endpoint, T data);
    Task<HttpResponseMessage> PutAsync<T>(string endpoint, T data);
    Task<HttpResponseMessage> DeleteAsync(string endpoint);
}

public class ApiService : IApiService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;

    public ApiService(HttpClient http, ILocalStorageService localStorage)
    {
        _http = http;
        _localStorage = localStorage;
    }

    private async Task AddAuthHeaderAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("accessToken");
        if (!string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task<HttpResponseMessage> GetAsync(string endpoint)
    {
        await AddAuthHeaderAsync();
        return await _http.GetAsync(endpoint);
    }

    public async Task<HttpResponseMessage> PostAsync<T>(string endpoint, T data)
    {
        await AddAuthHeaderAsync();
        return await _http.PostAsJsonAsync(endpoint, data);
    }

    public async Task<HttpResponseMessage> PutAsync<T>(string endpoint, T data)
    {
        await AddAuthHeaderAsync();
        return await _http.PutAsJsonAsync(endpoint, data);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string endpoint)
    {
        await AddAuthHeaderAsync();
        return await _http.DeleteAsync(endpoint);
    }
}
