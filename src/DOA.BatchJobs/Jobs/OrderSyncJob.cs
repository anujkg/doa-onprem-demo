using Microsoft.Data.SqlClient;

namespace DOA.BatchJobs.Jobs;

/// <summary>
/// Syncs order statuses with the external Mercury application via its REST API.
/// ⚠️ MIGRATION: This already uses HTTP — minimal changes needed for Azure.
///              Main change: connection string → Managed Identity for SQL.
/// </summary>
public class OrderSyncJob
{
    private readonly string _connectionString;

    public OrderSyncJob(string connectionString) => _connectionString = connectionString;

    public async Task<int> RunAsync()
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        // Get pending orders to sync
        var sql = "SELECT Id, ExternalRefId FROM Orders WHERE SyncStatus = 'Pending'";
        using var cmd = new SqlCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();

        var pendingOrders = new List<(int Id, string ExtRef)>();
        while (await reader.ReadAsync())
        {
            pendingOrders.Add((reader.GetInt32(0), reader.GetString(1)));
        }
        await reader.CloseAsync();

        Console.WriteLine($"[{DateTime.Now}] Found {pendingOrders.Count} orders to sync");

        var synced = 0;
        using var httpClient = new HttpClient();

        foreach (var (id, extRef) in pendingOrders)
        {
            try
            {
                // Call Mercury API to get latest status
                var response = await httpClient.GetStringAsync(
                    $"https://mercury-api.partner.com/orders/{extRef}/status");

                // Update local DB
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
                Console.WriteLine($"[{DateTime.Now}] Sync failed for order {id}: {ex.Message}");
            }
        }

        return synced;
    }
}
