using Microsoft.Data.SqlClient;

namespace DOA.BatchJobs.Jobs;

/// <summary>
/// Archives orders older than 2 years to the archive table.
/// Runs as part of nightly batch to keep the main Orders table performant.
/// 
/// ⚠️ MIGRATION: SQL connection → Managed Identity; consider Azure SQL auto-archive policies
/// </summary>
public class DataCleanupJob
{
    private readonly string _connectionString;

    public DataCleanupJob(string connectionString) => _connectionString = connectionString;

    public async Task<int> RunAsync()
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        // Archive old orders
        var archiveSql = @"
            INSERT INTO Orders_Archive 
            SELECT * FROM Orders 
            WHERE CreatedAt < DATEADD(YEAR, -2, GETDATE()) 
            AND Status IN ('Delivered', 'Cancelled')";

        using var archiveCmd = new SqlCommand(archiveSql, conn);
        var archived = await archiveCmd.ExecuteNonQueryAsync();

        // Delete archived orders from main table
        var deleteSql = @"
            DELETE FROM Orders 
            WHERE CreatedAt < DATEADD(YEAR, -2, GETDATE()) 
            AND Status IN ('Delivered', 'Cancelled')";

        using var deleteCmd = new SqlCommand(deleteSql, conn);
        await deleteCmd.ExecuteNonQueryAsync();

        Console.WriteLine($"[{DateTime.Now}] Archived and cleaned {archived} orders");

        return archived;
    }
}
