namespace blazor_jwt_auth.Email;

public interface ICustomEmailSender<TUser> where TUser : class
{
    public Task SendDefaultPasswordAsync(TUser user, string email, string password, string subject);
}