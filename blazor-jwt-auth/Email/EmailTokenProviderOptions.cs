using Microsoft.AspNetCore.Identity;

namespace blazor_jwt_auth.Email;

public class EmailTokenProviderOptions : DataProtectionTokenProviderOptions
{
    public EmailTokenProviderOptions()
    {
        Name = "EmailTokenProvider";
        TokenLifespan = TimeSpan.FromHours(1);
    }
}