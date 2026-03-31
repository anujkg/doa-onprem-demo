using System.Net;
using System.Net.Mail;
using Microsoft.Data.SqlClient;

namespace DOA.BatchJobs.Jobs;

/// <summary>
/// Sends nightly email digest to customers with order status updates.
/// Uses on-prem SMTP relay — no auth required on internal network.
/// 
/// ⚠️ MIGRATION TARGET: Azure Communication Services (Email)
/// </summary>
public class EmailDigestJob
{
    private readonly string _connectionString;
    private readonly string _smtpHost;

    public EmailDigestJob(string connectionString, string smtpHost)
    {
        _connectionString = connectionString;
        _smtpHost = smtpHost;
    }

    public async Task<int> RunAsync()
    {
        using var conn = new SqlConnection(_connectionString);
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

        Console.WriteLine($"[{DateTime.Now}] Sending digest to {recipients.Count} customers");

        using var smtpClient = new SmtpClient(_smtpHost, 25)
        {
            EnableSsl = false,   // ⚠️ No TLS on internal relay
            Credentials = CredentialCache.DefaultNetworkCredentials
        };

        var sent = 0;
        foreach (var (email, name) in recipients)
        {
            try
            {
                var message = new MailMessage(
                    "noreply@doa.local", email,
                    "Your DOA Order Update",
                    $"Hi {name},\n\nYour recent order status has been updated. " +
                    $"Please log in to the DOA portal to view details.\n\nThank you.");

                await smtpClient.SendMailAsync(message);

                // Mark as notified
                var updateSql = $"UPDATE Orders SET NotificationSent = 1 WHERE CustomerEmail = '{email}'";  // ⚠️ SQL injection
                using var updateCmd = new SqlCommand(updateSql, conn);
                await updateCmd.ExecuteNonQueryAsync();

                sent++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] Failed to send email to {email}: {ex.Message}");
            }
        }

        return sent;
    }
}
