using Azure.Communication.Email;
using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;
using Serilog;

namespace DOA.BatchJobs.Jobs;

/// <summary>
/// Sends nightly email digest via Azure Communication Services.
/// Uses DefaultAzureCredential (Managed Identity) — no secrets required.
/// All SQL queries are parameterized to prevent injection.
/// </summary>
public class EmailDigestJob
{
    private readonly string _connectionString;
    private readonly string _acsEndpoint;
    private readonly string _fromAddress;
    private readonly TokenCredential _credential;

    public EmailDigestJob(string connectionString, string acsEndpoint, string fromAddress, TokenCredential credential)
    {
        _connectionString = connectionString;
        _acsEndpoint = acsEndpoint;
        _fromAddress = fromAddress;
        _credential = credential;
    }

    public async Task<int> RunAsync()
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();

        var sql = @"SELECT DISTINCT CustomerEmail, CustomerName
                    FROM Orders
                    WHERE Status IN ('Shipped', 'Delivered')
                    AND NotificationSent = 0
                    AND CreatedAt > DATEADD(DAY, -1, GETDATE())";

        using var cmd = new SqlCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();

        var recipients = new List<(string Email, string Name)>();
        while (await reader.ReadAsync())
        {
            recipients.Add((reader.GetString(0), reader.GetString(1)));
        }
        await reader.CloseAsync();

        Log.Information("Sending digest to {Count} customers", recipients.Count);

        var emailClient = new EmailClient(new Uri(_acsEndpoint), _credential);
        var sent = 0;

        foreach (var (email, name) in recipients)
        {
            try
            {
                var emailMessage = new EmailMessage(
                    senderAddress: _fromAddress,
                    recipients: new EmailRecipients(new List<EmailAddress> { new EmailAddress(email) }),
                    content: new EmailContent("Your DOA Order Update")
                    {
                        PlainText = $"Hi {name},\n\nYour recent order status has been updated. " +
                                    $"Please log in to the DOA portal to view details.\n\nThank you."
                    });

                await emailClient.SendAsync(Azure.WaitUntil.Completed, emailMessage);

                // Parameterized update — no SQL injection risk
                var updateSql = "UPDATE Orders SET NotificationSent = 1 WHERE CustomerEmail = @Email";
                using var updateCmd = new SqlCommand(updateSql, conn);
                updateCmd.Parameters.AddWithValue("@Email", email);
                await updateCmd.ExecuteNonQueryAsync();

                sent++;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to send email to {Email}", email);
            }
        }

        return sent;
    }

    private SqlConnection CreateConnection()
    {
        var conn = new SqlConnection(_connectionString);
        if (!_connectionString.Contains("Password") && !_connectionString.Contains("Pwd"))
        {
            var credential = new DefaultAzureCredential();
            var tokenRequestContext = new Azure.Core.TokenRequestContext(
                new[] { "https://database.windows.net/.default" });
            var tokenResult = credential.GetToken(tokenRequestContext);
            conn.AccessToken = tokenResult.Token;
        }
        return conn;
    }
}
