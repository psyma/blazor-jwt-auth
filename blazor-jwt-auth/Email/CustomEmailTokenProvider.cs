using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace blazor_jwt_auth.Email;

public class CustomEmailTokenProvider<TUser> : DataProtectorTokenProvider<TUser> where TUser : class
{
    public CustomEmailTokenProvider(
        IDataProtectionProvider dataProtectionProvider, 
        IOptions<EmailTokenProviderOptions> options,
        ILogger<DataProtectorTokenProvider<TUser>> logger) : base(dataProtectionProvider, options, logger) { }
}