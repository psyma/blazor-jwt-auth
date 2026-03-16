using MessagePack;

namespace blazor_jwt_auth.Client.Models;

[MessagePackObject]
public class AuthResponse
{
    [Key(0)] public string AccessToken { get; set; } = string.Empty;
    [Key(1)] public string RefreshToken { get; set; } = string.Empty;
    [Key(2)] public DateTime AccessTokenExpiresAt { get; set; }
}