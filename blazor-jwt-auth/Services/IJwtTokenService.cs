using blazor_jwt_auth.Data;

namespace blazor_jwt_auth.Services;

public interface IJwtTokenService
{
    Task<string> CreateAccessToken(ApplicationUser user);
    string CreateRefreshToken();
}