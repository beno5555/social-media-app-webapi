using aspnetproject.Infrastructure.Dtos.Auth.Email;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace aspnetproject.Infrastructure.Services.Helpers;

public class EmailSender
{
    private readonly EmailConfiguration _emailConfiguration;

    public EmailSender(IOptions<EmailConfiguration> emailConfiguration)
    {
        _emailConfiguration = emailConfiguration.Value;
    }

    public async Task<bool> SendAsync(string to, string subject, string body)
    {
        bool sentEmail = true;
        try
        {
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(_emailConfiguration.SenderName, _emailConfiguration.SenderEmail));
            message.To.Add(MailboxAddress.Parse(to));

            message.Subject = subject;

            message.Body = new BodyBuilder { HtmlBody = body }.ToMessageBody();

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(_emailConfiguration.SmtpHost, _emailConfiguration.SmtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_emailConfiguration.Username, _emailConfiguration.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }
        catch (Exception)
        {
            sentEmail = false;
        }
        
        return sentEmail;
    }
}