using System.Net.Http.Json;
using Blazored.LocalStorage;
using SmartCutScheduler.Web.Models;
using SmartCutScheduler.Web.State;
using Microsoft.AspNetCore.Components.Authorization;

namespace SmartCutScheduler.Web.Services;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
    Task LogoutAsync();
    Task<UserDto?> GetCurrentUserAsync();
}

public class AuthService : IAuthService
{
    private readonly IApiService _api;
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authStateProvider;

    public AuthService(IApiService api, ILocalStorageService localStorage, AuthenticationStateProvider authStateProvider)
    {
        _api = api;
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var response = await _api.PostAsync("/api/auth/login", request);
        if (response.IsSuccessStatusCode)
        {
            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (authResponse != null)
            {
                await _localStorage.SetItemAsync("accessToken", authResponse.AccessToken);
                await _localStorage.SetItemAsync("refreshToken", authResponse.RefreshToken);
                ((CustomAuthStateProvider)_authStateProvider).NotifyUserAuthentication(authResponse.AccessToken);
            }
            return authResponse;
        }
        return null;
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        var response = await _api.PostAsync("/api/auth/register", request);
        if (response.IsSuccessStatusCode)
        {
            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (authResponse != null)
            {
                await _localStorage.SetItemAsync("accessToken", authResponse.AccessToken);
                await _localStorage.SetItemAsync("refreshToken", authResponse.RefreshToken);
                ((CustomAuthStateProvider)_authStateProvider).NotifyUserAuthentication(authResponse.AccessToken);
            }
            return authResponse;
        }
        return null;
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync("accessToken");
        await _localStorage.RemoveItemAsync("refreshToken");
        ((CustomAuthStateProvider)_authStateProvider).NotifyUserLogout();
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("accessToken");
        if (string.IsNullOrEmpty(token))
            return null;

        // TODO: Decode JWT or call API to get user info
        return null;
    }
}
