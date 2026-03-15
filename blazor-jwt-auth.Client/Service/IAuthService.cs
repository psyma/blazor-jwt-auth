namespace blazor_jwt_auth.Client.Service;

public interface IAuthService
{
    Task<bool> Login(string email,  string password);
    Task<bool> Register(string email, string password);
    Task<bool> Refresh();
    Task Logout();
    Task<bool> TryRestoreSessionAsync();
    Task InitializeAsync();
}