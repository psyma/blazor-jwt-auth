using System.Net.Http.Json;
using blazor_jwt_auth.Client.Data;
using blazor_jwt_auth.Client.Models;
using Blazored.LocalStorage;

namespace blazor_jwt_auth.Client.Service;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly JwtAuthStateProvider _authStateProvider;
    public bool IsInitialized { get; private set; }
    
    public AuthService(
        HttpClient httpClient,
        ILocalStorageService localStorage,
        JwtAuthStateProvider authStateProvider)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
    }
    
    public async Task<bool> Login(string email, string password)
    {
        var response = await _httpClient.PostAsJsonAsync("api/v1/auth/login", new
        {
            Email = email,
            Password = password
        });

        if (!response.IsSuccessStatusCode)
            return false;

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();

        await _localStorage.SetItemAsync("authToken", result!.AccessToken);
        await _localStorage.SetItemAsync("refreshToken", result.RefreshToken);

        _authStateProvider.NotifyUserAuthentication(result.AccessToken);
        IsInitialized = true;
        return true;
    }

    public async Task<bool> Register(string email, string password)
    {
        var response = await _httpClient.PostAsJsonAsync("api/v1/auth/register", new
        {
            Email = email,
            Password = password
        });

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> Refresh()
    {
        var refreshToken = await _localStorage.GetItemAsync<string>("refreshToken");

        var response = await _httpClient.PostAsJsonAsync("api/v1/auth/refresh", new
        {
            RefreshToken = refreshToken
        });

        if (!response.IsSuccessStatusCode)
            return false;

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();

        await _localStorage.SetItemAsync("authToken", result!.AccessToken);
        await _localStorage.SetItemAsync("refreshToken", result.RefreshToken);

        _authStateProvider.NotifyUserAuthentication(result.AccessToken);
        return true;
    }

    public async Task Logout()
    {
        await _localStorage.RemoveItemAsync("authToken");
        await _localStorage.RemoveItemAsync("refreshToken");

        _authStateProvider.NotifyUserLogout();
        IsInitialized = true;
    }
    
    public async Task<bool> TryRestoreSessionAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("authToken");

        if (!string.IsNullOrWhiteSpace(token))
        {
            if (!JwtParser.IsTokenExpired(token))
            {
                _authStateProvider.NotifyUserAuthentication(token);
                return true;
            }
        }

        return await Refresh();
    }
    
    public async Task InitializeAsync()
    {
        if (IsInitialized)
            return;

        await TryRestoreSessionAsync();
        IsInitialized = true;
    }
}