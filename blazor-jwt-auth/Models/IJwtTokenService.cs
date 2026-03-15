using blazor_jwt_auth.Data;

namespace blazor_jwt_auth.Models;

public interface IJwtTokenService
{
    Task<string> CreateAccessToken(ApplicationUser user);
    string CreateRefreshToken();
}