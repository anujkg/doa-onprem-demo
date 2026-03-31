using DOA.WebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DOA.WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderRepository _orderRepo;
    private readonly IEmailService _emailService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderRepository orderRepo, IEmailService emailService,
        ILogger<OrdersController> logger)
    {
        _orderRepo = orderRepo;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] string? status = null)
    {
        _logger.LogInformation("GET /api/orders — User: {User}", User.Identity?.Name);

        var orders = await _orderRepo.GetOrdersAsync(status);
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        _logger.LogInformation("GET /api/orders/{OrderId} — User: {User}", id, User.Identity?.Name);

        var order = await _orderRepo.GetOrderByIdAsync(id);
        if (order == null) return NotFound();
        return Ok(order);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        _logger.LogInformation("POST /api/orders — User: {User}", User.Identity?.Name);

        var orderId = await _orderRepo.CreateOrderAsync(request);

        await _emailService.SendAsync(
            to: request.CustomerEmail,
            subject: $"Order #{orderId} Confirmed",
            body: $"Your order has been placed. Order ID: {orderId}");

        _logger.LogInformation("Order {OrderId} created, confirmation email sent to {Email}", orderId, request.CustomerEmail.Replace("\r", "").Replace("\n", ""));
        return CreatedAtAction(nameof(GetOrder), new { id = orderId }, new { OrderId = orderId });
    }

    [HttpPut("{id}/status")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        _logger.LogInformation("PUT /api/orders/{OrderId}/status — User: {User}", id, User.Identity?.Name);

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
