using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using blazor_jwt_auth.Client.Data;
using blazor_jwt_auth.Client.Models;
using MessagePack;

namespace blazor_jwt_auth.Client.Service;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly JwtAuthStateProvider _authStateProvider;
    private bool IsInitialized { get; set; }
    
    public AuthService(HttpClient httpClient, JwtAuthStateProvider authStateProvider)
    {
        _httpClient = httpClient;
        _authStateProvider = authStateProvider;
    }
    
    public async Task<bool> Login(string email, string password)
    {
        var payload = new
        {
            Email = email,
            Password = password
        };
        var response = await _httpClient.PostAsJsonAsync("api/v1/auth/login", payload);
        if (!response.IsSuccessStatusCode) return false;

        var contentStream = await response.Content.ReadAsStreamAsync();
        var authResponse = await MessagePackSerializer.DeserializeAsync<AuthResponse>(contentStream);

        _authStateProvider.NotifyUserAuthentication(authResponse.AccessToken);
        IsInitialized = true;
        return true;
    }

    public async Task<bool> Register(string email, string password)
    {
        var payload = new
        {
            Email = email,
            Password = password
        };
        var response = await _httpClient.PostAsJsonAsync("api/v1/auth/register", payload);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> Refresh()
    {
        var response = await _httpClient.PostAsJsonAsync("api/v1/auth/refresh", new {});
        if (!response.IsSuccessStatusCode)
            return false;
        
        var contentStream = await response.Content.ReadAsStreamAsync();
        var authResponse = await MessagePackSerializer.DeserializeAsync<AuthResponse>(contentStream);
        
        _authStateProvider.NotifyUserAuthentication(authResponse.AccessToken);
        return true;
    }
    
    public async Task Logout(string? email)
    {
        await _httpClient.PostAsync($"api/v1/auth/logout?email={email}", null);
        
        _authStateProvider.NotifyUserLogout();
        IsInitialized = true;
    }
    
    public async Task InitializeAsync()
    {
        if (IsInitialized) return;

        await Refresh();
        IsInitialized = true;
    }

    public async Task<string> Test()
    {
        var response = await _httpClient.GetAsync("api/v1/auth/test");
        
        return await response.Content.ReadAsStringAsync();
    }
    
    public void ExternalLogin(string accessToken)
    {
        _authStateProvider.NotifyUserAuthentication(accessToken);
        IsInitialized = true;
    }
}