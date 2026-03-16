using blazor_jwt_auth.Data;
using blazor_jwt_auth.Models;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace blazor_jwt_auth.Email;

public class CustomEmailSender : ICustomEmailSender<ApplicationUser>
{
    private readonly EmailSettings _emailSettings;

    public CustomEmailSender(IOptions<EmailSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
    }

    public async Task SendDefaultPasswordAsync(ApplicationUser user, string email, string password, string subject)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(MailboxAddress.Parse(_emailSettings.Username));
        mimeMessage.To.Add(MailboxAddress.Parse(email));
        mimeMessage.Subject = subject;
        mimeMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html)
        {
            Text = "Text message"
        };
        
        await SendEmailAsync(mimeMessage); 
    }
    
    private async Task SendEmailAsync(MimeMessage mimeMessage)
    {
        var smtp = new SmtpClient();
        await smtp.ConnectAsync(_emailSettings.Host, _emailSettings.Port, MailKit.Security.SecureSocketOptions.SslOnConnect);
        await smtp.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
        await smtp.SendAsync(mimeMessage);
        await smtp.DisconnectAsync(true); 
    }
}