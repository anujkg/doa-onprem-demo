using Microsoft.Data.SqlClient;
using Serilog;

namespace DOA.BatchJobs.Jobs;

/// <summary>
/// Syncs order statuses with the external Mercury application via its REST API.
/// Uses DefaultAzureCredential for SQL Server Managed Identity authentication.
/// </summary>
public class OrderSyncJob
{
    private readonly string _connectionString;

    public OrderSyncJob(string connectionString) => _connectionString = connectionString;

    public async Task<int> RunAsync()
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();

        var sql = "SELECT Id, ExternalRefId FROM Orders WHERE SyncStatus = 'Pending'";
        using var cmd = new SqlCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();

        var pendingOrders = new List<(int Id, string ExtRef)>();
        while (await reader.ReadAsync())
        {
            pendingOrders.Add((reader.GetInt32(0), reader.GetString(1)));
        }
        await reader.CloseAsync();

        Log.Information("Found {Count} orders to sync with Mercury", pendingOrders.Count);

        var synced = 0;
        using var httpClient = new HttpClient();

        foreach (var (id, extRef) in pendingOrders)
        {
            try
            {
                var response = await httpClient.GetStringAsync(
                    $"https://mercury-api.partner.com/orders/{Uri.EscapeDataString(extRef)}/status");

                var updateSql = @"UPDATE Orders SET SyncStatus = 'Synced',
                                  ExternalStatus = @Status, LastSyncDate = GETDATE()
                                  WHERE Id = @Id";
                using var updateCmd = new SqlCommand(updateSql, conn);
                updateCmd.Parameters.AddWithValue("@Status", response);
                updateCmd.Parameters.AddWithValue("@Id", id);
                await updateCmd.ExecuteNonQueryAsync();

                synced++;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Sync failed for order {OrderId}", id);
            }
        }

        return synced;
    }

    private SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}
