using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;

namespace dsstats.api.Services;

public class EMailService(IOptions<EMailOptions> options, ILogger<EMailService> logger)
{
    public async Task SendEmail(string receiver, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("dsstats", options.Value.Email));
        message.To.Add(new MailboxAddress(receiver, receiver));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        try
        {
            using var client = new SmtpClient();
            client.Connect(options.Value.Smtp, options.Value.Port, MailKit.Security.SecureSocketOptions.StartTls);
            client.Authenticate(options.Value.Email, options.Value.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            logger.LogError("failed sending email to {receiver}: {error}", receiver, ex.Message);
        }
    }
}

public class EmailSender(IOptions<EMailOptions> options, ILogger<EmailSender> logger) : IEmailSender
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("dsstats", options.Value.Email));
        message.To.Add(new MailboxAddress(email, email));
        message.Subject = subject;
        // message.Body = new TextPart("plain") { Text = htmlMessage };
        message.Body = new TextPart("html") { Text = htmlMessage };

        try
        {
            using var client = new SmtpClient();
            client.Connect(options.Value.Smtp, options.Value.Port, MailKit.Security.SecureSocketOptions.StartTls);
            client.Authenticate(options.Value.Email, options.Value.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            logger.LogError("failed sending email to {receiver}: {error}", email, ex.Message);
        }
    }
}

public record EMailOptions
{
    public string Email { get; set; } = string.Empty;
    public string Smtp { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Password { get; set; } = string.Empty;
}

