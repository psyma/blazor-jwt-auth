using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace blazor_jwt_auth.Client.Data;

public class JwtAuthStateProvider : AuthenticationStateProvider
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());
    private ClaimsPrincipal? _user;
    private string? _token;
    public string? GetToken() => _token;
    
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(_user == null ? new AuthenticationState(Anonymous) : new AuthenticationState(_user));
    }
    
    public void NotifyUserAuthentication(string token)
    {
        var claims = JwtParser.ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        _user = new ClaimsPrincipal(identity);
        _token = token;
        
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_user)));
    }

    public void NotifyUserLogout()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymous)));
    } 
}