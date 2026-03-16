using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using blazor_jwt_auth.Client.Data;
using blazor_jwt_auth.Client.Models;

namespace blazor_jwt_auth.Client.Service;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly JwtAuthStateProvider _authStateProvider;
    private bool IsInitialized { get; set; }
    
    public AuthService(
        HttpClient httpClient,
        JwtAuthStateProvider authStateProvider)
    {
        _httpClient = httpClient;
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

        if (result != null)
        {
            _authStateProvider.NotifyUserAuthentication(result.AccessToken);
            IsInitialized = true;
            return true;
        }
        
        return false;
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
        var response = await _httpClient.PostAsJsonAsync("api/v1/auth/refresh", new
        {
            RefreshToken = string.Empty
        });

        if (!response.IsSuccessStatusCode)
            return false;

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            
        if (result != null)
        {
            _authStateProvider.NotifyUserAuthentication(result.AccessToken);
            return true;
        }
        
        return false;
    }

    public async Task Logout(string? email)
    {
        await _httpClient.PostAsync($"api/v1/auth/logout?email={email}", null);
        
        _authStateProvider.NotifyUserLogout();
        IsInitialized = true;
    }
    
    public async Task InitializeAsync()
    {
        if (IsInitialized)
            return;

        await Refresh();
        IsInitialized = true;
    }

    public async Task<string> Test()
    {
        var response = await _httpClient.GetAsync("api/v1/auth/test");
        
        return await response.Content.ReadAsStringAsync();
    }
}