using DOA.WebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DOA.WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderRepository _orderRepo;
    private readonly IEmailService _emailService;

    public OrdersController(IOrderRepository orderRepo, IEmailService emailService)
    {
        _orderRepo = orderRepo;
        _emailService = emailService;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] string? status = null)
    {
        Console.WriteLine($"[{DateTime.Now}] GET /api/orders — User: {User.Identity?.Name}");

        var orders = await _orderRepo.GetOrdersAsync(status);
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        Console.WriteLine($"[{DateTime.Now}] GET /api/orders/{id} — User: {User.Identity?.Name}");

        var order = await _orderRepo.GetOrderByIdAsync(id);
        if (order == null) return NotFound();
        return Ok(order);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        Console.WriteLine($"[{DateTime.Now}] POST /api/orders — User: {User.Identity?.Name}");

        var orderId = await _orderRepo.CreateOrderAsync(request);

        // Send confirmation via on-prem SMTP
        await _emailService.SendAsync(
            to: request.CustomerEmail,
            subject: $"Order #{orderId} Confirmed",
            body: $"Your order has been placed. Order ID: {orderId}"
        );

        Console.WriteLine($"[{DateTime.Now}] Order {orderId} created, email sent to {request.CustomerEmail}");
        return CreatedAtAction(nameof(GetOrder), new { id = orderId }, new { OrderId = orderId });
    }

    [HttpPut("{id}/status")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        Console.WriteLine($"[{DateTime.Now}] PUT /api/orders/{id}/status — User: {User.Identity?.Name}");

        await _orderRepo.UpdateOrderStatusAsync(id, request.Status);
        return NoContent();
    }
}

// Request models
public record CreateOrderRequest(
    string CustomerName,
    string CustomerEmail,
    string ProductCode,
    int Quantity,
    decimal UnitPrice
);

public record UpdateStatusRequest(string Status);
