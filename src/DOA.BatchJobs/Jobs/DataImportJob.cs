using Microsoft.Data.SqlClient;

namespace DOA.BatchJobs.Jobs;

/// <summary>
/// Nightly data import — reads CSV from network share and inserts into SQL Server.
/// ⚠️ MIGRATION: Network share path → Azure Blob Storage; SQL → Azure SQL with Managed Identity
/// </summary>
public class DataImportJob
{
    private readonly string _connectionString;

    public DataImportJob(string connectionString) => _connectionString = connectionString;

    public async Task<int> RunAsync()
    {
        // ⚠️ UNC path to on-prem file share
        var importPath = @"\\fileserver01.doa.local\DataFeeds\daily_orders.csv";

        Console.WriteLine($"[{DateTime.Now}] Reading import file: {importPath}");

        if (!File.Exists(importPath))
        {
            Console.WriteLine($"[{DateTime.Now}] WARNING: Import file not found, skipping");
            return 0;
        }

        var lines = await File.ReadAllLinesAsync(importPath);
        var count = 0;

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        foreach (var line in lines.Skip(1)) // skip header
        {
            var parts = line.Split(',');
            if (parts.Length < 5) continue;

            // ⚠️ Using string concatenation — SQL injection risk in batch job
            var sql = $@"
                INSERT INTO ImportedOrders (ExternalId, CustomerName, Product, Quantity, ImportDate)
                VALUES ('{parts[0]}', '{parts[1]}', '{parts[2]}', {parts[3]}, GETDATE())";

            using var cmd = new SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
            count++;
        }

        return count;
    }
}
