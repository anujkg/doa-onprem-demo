using System.Net;
using System.Net.Mail;

namespace DOA.WebApp.Services;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body);
}

/// <summary>
/// On-prem SMTP relay for email notifications.
/// Uses internal mail server — no authentication required on internal network.
/// 
/// ⚠️ MIGRATION TARGET: Replace with Azure Communication Services (Email)
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _fromAddress;

    public SmtpEmailService(string smtpHost, int smtpPort, string fromAddress)
    {
        _smtpHost = smtpHost;
        _smtpPort = smtpPort;
        _fromAddress = fromAddress;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        Console.WriteLine($"[{DateTime.Now}] Sending email via {_smtpHost}:{_smtpPort} to {to}");

        using var client = new SmtpClient(_smtpHost, _smtpPort)
        {
            EnableSsl = false,                    // ⚠️ No TLS on internal relay
            Credentials = CredentialCache.DefaultNetworkCredentials,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 30000
        };

        var message = new MailMessage(
            from: new MailAddress(_fromAddress, "DOA Order System"),
            to: new MailAddress(to))
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };

        await client.SendMailAsync(message);
        Console.WriteLine($"[{DateTime.Now}] Email sent successfully to {to}");
    }
}
