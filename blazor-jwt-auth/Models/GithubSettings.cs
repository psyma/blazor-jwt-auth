namespace blazor_jwt_auth.Models;

public class GithubSettings
{
    public required string Id { get; set; }
    public required string Secret { get; set; }
    public required string CallbackPath { get; set; }
    public required string AuthorizationEndpoint { get; set; }
    public required string TokenEndpoint { get; set; }
    public required string UserInformationEndpoint { get; set; }
}