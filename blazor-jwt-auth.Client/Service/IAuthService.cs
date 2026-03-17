namespace blazor_jwt_auth.Client.Service;

public interface IAuthService
{
    Task<bool> Login(string email,  string password);
    Task<bool> Register(string email, string password);
    Task<bool> Refresh();
    Task Logout(string? email);
    Task InitializeAsync();
    Task<string> Test();
    void ExternalLogin(string accessToken);
}