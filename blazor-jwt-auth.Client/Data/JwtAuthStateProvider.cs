using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace blazor_jwt_auth.Client.Data;

public class JwtAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private static readonly ClaimsPrincipal Anonymous =
        new(new ClaimsIdentity());
    
    public JwtAuthStateProvider(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }
    
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // IMPORTANT: don't touch localStorage here
        return Task.FromResult(new AuthenticationState(Anonymous));
    }

    public void NotifyUserAuthentication(string token)
    {
        var claims = JwtParser.ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(user)));
    }

    public void NotifyUserLogout()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(anonymous)));
    }
}