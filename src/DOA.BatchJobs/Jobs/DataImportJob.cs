using Azure.Storage.Blobs;
using Microsoft.Data.SqlClient;
using Serilog;

namespace DOA.BatchJobs.Jobs;

/// <summary>
/// Nightly data import — reads CSV from Azure Blob Storage and inserts into Azure SQL.
/// Uses DefaultAzureCredential for both Blob Storage and SQL Server.
/// All SQL uses parameterized queries to prevent injection.
/// </summary>
public class DataImportJob
{
    private readonly string _connectionString;
    private readonly BlobServiceClient _blobServiceClient;

    public DataImportJob(string connectionString, BlobServiceClient blobServiceClient)
    {
        _connectionString = connectionString;
        _blobServiceClient = blobServiceClient;
    }

    public async Task<int> RunAsync()
    {
        // Azure Blob Storage replacing on-prem NFS file share
        const string containerName = "data-feeds";
        const string blobName = "daily_orders.csv";

        Log.Information("Reading import file from Azure Blob Storage: {Container}/{Blob}", containerName, blobName);

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync())
        {
            Log.Warning("Import file not found in blob storage: {Container}/{Blob}, skipping", containerName, blobName);
            return 0;
        }

        var download = await blobClient.DownloadContentAsync();
        var content = download.Value.Content.ToString();
        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        var count = 0;
        using var conn = CreateConnection();
        await conn.OpenAsync();

        foreach (var line in lines.Skip(1)) // skip header
        {
            var parts = line.Split(',');
            if (parts.Length < 5) continue;

            // Parameterized query — no SQL injection risk
            var sql = @"
                INSERT INTO ImportedOrders (ExternalId, CustomerName, Product, Quantity, ImportDate)
                VALUES (@ExternalId, @CustomerName, @Product, @Quantity, GETDATE())";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ExternalId", parts[0].Trim());
            cmd.Parameters.AddWithValue("@CustomerName", parts[1].Trim());
            cmd.Parameters.AddWithValue("@Product", parts[2].Trim());
            cmd.Parameters.AddWithValue("@Quantity", int.TryParse(parts[3].Trim(), out var qty) ? qty : 0);

            await cmd.ExecuteNonQueryAsync();
            count++;
        }

        return count;
    }

    private SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}
