using DOA.WebApp.Auth;
using DOA.WebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DOA.WebApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "ReportViewer")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILdapAuthService _ldapService;

    public ReportsController(IReportService reportService, ILdapAuthService ldapService)
    {
        _reportService = reportService;
        _ldapService = ldapService;
    }

    [HttpGet("monthly-sales")]
    public async Task<IActionResult> GetMonthlySalesReport(
        [FromQuery] int year,
        [FromQuery] int month)
    {
        var username = User.Identity?.Name ?? "unknown";
        Console.WriteLine($"[{DateTime.Now}] Report request: MonthlySales {year}-{month} by {username}");

        // Check LDAP group membership for report access
        var groups = _ldapService.GetUserGroups(username);
        if (!groups.Contains("DOA\\ReportViewers"))
        {
            Console.WriteLine($"[{DateTime.Now}] Report access denied for {username}");
            return Forbid();
        }

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
        Console.WriteLine($"[{DateTime.Now}] Report request: Inventory by {User.Identity?.Name}");

        var reportBytes = await _reportService.RenderReportAsync(
            reportPath: "/DOA Reports/Inventory Status",
            parameters: new Dictionary<string, string>());

        return File(reportBytes, "application/pdf", "InventoryStatus.pdf");
    }
}
