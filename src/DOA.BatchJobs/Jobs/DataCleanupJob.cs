using Azure.Identity;
using Microsoft.Data.SqlClient;
using Serilog;

namespace DOA.BatchJobs.Jobs;

/// <summary>
/// Archives orders older than 2 years to the archive table.
/// Uses DefaultAzureCredential (Managed Identity) for Azure SQL authentication.
/// </summary>
public class DataCleanupJob
{
    private readonly string _connectionString;

    public DataCleanupJob(string connectionString) => _connectionString = connectionString;

    public async Task<int> RunAsync()
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();

        var archiveSql = @"
            INSERT INTO Orders_Archive
            SELECT * FROM Orders
            WHERE CreatedAt < DATEADD(YEAR, -2, GETDATE())
            AND Status IN ('Delivered', 'Cancelled')";

        using var archiveCmd = new SqlCommand(archiveSql, conn);
        var archived = await archiveCmd.ExecuteNonQueryAsync();

        var deleteSql = @"
            DELETE FROM Orders
            WHERE CreatedAt < DATEADD(YEAR, -2, GETDATE())
            AND Status IN ('Delivered', 'Cancelled')";

        using var deleteCmd = new SqlCommand(deleteSql, conn);
        await deleteCmd.ExecuteNonQueryAsync();

        Log.Information("Archived and cleaned {Count} orders", archived);

        return archived;
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
