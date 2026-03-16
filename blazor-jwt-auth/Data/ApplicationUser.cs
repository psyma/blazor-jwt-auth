using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace blazor_jwt_auth.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser<int>
{
    [MaxLength(120)]
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
}