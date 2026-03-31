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
        return new SqlConnection(_connectionString);
    }
}
