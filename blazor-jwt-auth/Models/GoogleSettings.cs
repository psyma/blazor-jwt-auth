namespace blazor_jwt_auth.Models;

public class GoogleSettings
{
    public required string Id { get; set; }
    public required string Secret { get; set; }
    public required string CallbackPath { get; set; }
    public required string CookieName { get; set; }
}