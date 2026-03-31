using DOA.WebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DOA.WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "ReportViewer")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    [HttpGet("monthly-sales")]
    public async Task<IActionResult> GetMonthlySalesReport(
        [FromQuery] int year,
        [FromQuery] int month)
    {
        var username = User.Identity?.Name ?? "unknown";
        _logger.LogInformation("Report request: MonthlySales {Year}-{Month} by {Username}", year, month, username);

        var reportBytes = await _reportService.RenderReportAsync(
            reportPath: "/DOA Reports/Monthly Sales",
            parameters: new Dictionary<string, string>
            {
                { "Year", year.ToString() },
                { "Month", month.ToString() }
            });

        return File(reportBytes, "application/pdf", $"MonthlySales_{year}_{month}.pdf");
    }

    [HttpGet("inventory")]
    public async Task<IActionResult> GetInventoryReport()
    {
        _logger.LogInformation("Report request: Inventory by {Username}", User.Identity?.Name);

        var reportBytes = await _reportService.RenderReportAsync(
            reportPath: "/DOA Reports/Inventory Status",
            parameters: new Dictionary<string, string>());

        return File(reportBytes, "application/pdf", "InventoryStatus.pdf");
    }
}
