using Azure.Communication.Email;
using Azure.Identity;
using Microsoft.Extensions.Logging;

namespace DOA.WebApp.Services;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body);
}

/// <summary>
/// Azure Communication Services email implementation.
/// Uses DefaultAzureCredential (Managed Identity) — no secrets required.
/// </summary>
public class AzureCommunicationEmailService : IEmailService
{
    private readonly EmailClient _emailClient;
    private readonly string _fromAddress;
    private readonly ILogger<AzureCommunicationEmailService> _logger;

    public AzureCommunicationEmailService(string acsEndpoint, string fromAddress,
        ILogger<AzureCommunicationEmailService> logger)
    {
        _emailClient = new EmailClient(new Uri(acsEndpoint), new DefaultAzureCredential());
        _fromAddress = fromAddress;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        _logger.LogInformation("Sending email to {Recipient} via Azure Communication Services", to);

        var emailMessage = new EmailMessage(
            senderAddress: _fromAddress,
            recipients: new EmailRecipients(new List<EmailAddress> { new EmailAddress(to) }),
            content: new EmailContent(subject) { PlainText = body });

        var operation = await _emailClient.SendAsync(Azure.WaitUntil.Completed, emailMessage);
        _logger.LogInformation("Email sent to {Recipient}, operation status: {Status}", to, operation.Value.Status);
    }
}
