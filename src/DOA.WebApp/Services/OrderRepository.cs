using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace DOA.WebApp.Services;

public interface IOrderRepository
{
    Task<List<OrderDto>> GetOrdersAsync(string? status);
    Task<OrderDto?> GetOrderByIdAsync(int id);
    Task<int> CreateOrderAsync(Controllers.CreateOrderRequest request);
    Task UpdateOrderStatusAsync(int id, string status);
}

public record OrderDto(int Id, string CustomerName, string ProductCode, int Quantity, decimal Total, string Status, DateTime CreatedAt);

/// <summary>
/// Data access layer — ADO.NET against Azure SQL Database.
/// Uses DefaultAzureCredential (Managed Identity) for authentication — no passwords.
/// All queries use parameterized statements to prevent SQL injection.
/// </summary>
public class OrderRepository : IOrderRepository
{
    private readonly string _connectionString;
    private readonly ILogger<OrderRepository> _logger;

    public OrderRepository(string connectionString, ILogger<OrderRepository> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }

    public async Task<List<OrderDto>> GetOrdersAsync(string? status)
    {
        var orders = new List<OrderDto>();

        using var conn = CreateConnection();
        await conn.OpenAsync();

        // Parameterized query — no SQL injection risk
        var sql = "SELECT Id, CustomerName, ProductCode, Quantity, Total, Status, CreatedAt FROM Orders";
        if (!string.IsNullOrEmpty(status))
        {
            sql += " WHERE Status = @Status";
        }
        sql += " ORDER BY CreatedAt DESC";

        using var cmd = new SqlCommand(sql, conn);
        if (!string.IsNullOrEmpty(status))
        {
            cmd.Parameters.AddWithValue("@Status", status);
        }

        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            orders.Add(new OrderDto(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3),
                reader.GetDecimal(4),
                reader.GetString(5),
                reader.GetDateTime(6)
            ));
        }

        return orders;
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int id)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = new SqlCommand(
            "SELECT Id, CustomerName, ProductCode, Quantity, Total, Status, CreatedAt FROM Orders WHERE Id = @Id",
            conn);
        cmd.Parameters.AddWithValue("@Id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new OrderDto(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetInt32(3),
            reader.GetDecimal(4),
            reader.GetString(5),
            reader.GetDateTime(6)
        );
    }

    public async Task<int> CreateOrderAsync(Controllers.CreateOrderRequest request)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();

        var sql = @"
            INSERT INTO Orders (CustomerName, CustomerEmail, ProductCode, Quantity, UnitPrice, Total, Status, CreatedAt)
            VALUES (@Name, @Email, @Product, @Qty, @Price, @Total, 'Pending', GETDATE());
            SELECT SCOPE_IDENTITY();";

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Name", request.CustomerName);
        cmd.Parameters.AddWithValue("@Email", request.CustomerEmail);
        cmd.Parameters.AddWithValue("@Product", request.ProductCode);
        cmd.Parameters.AddWithValue("@Qty", request.Quantity);
        cmd.Parameters.AddWithValue("@Price", request.UnitPrice);
        cmd.Parameters.AddWithValue("@Total", request.Quantity * request.UnitPrice);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task UpdateOrderStatusAsync(int id, string status)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = new SqlCommand(
            "UPDATE Orders SET Status = @Status WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Status", status);
        cmd.Parameters.AddWithValue("@Id", id);

        await cmd.ExecuteNonQueryAsync();
        _logger.LogInformation("Order {OrderId} status updated to {Status}", id, status);
    }
}
