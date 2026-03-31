namespace DOA.WebApp.Services;

public interface IReportService
{
    Task<byte[]> RenderReportAsync(string reportPath, Dictionary<string, string> parameters);
}

/// <summary>
/// SSRS (SQL Server Reporting Services) integration.
/// Calls the on-prem SSRS report server to render PDF reports.
/// 
/// ⚠️ MIGRATION TARGET: Replace with Power BI Paginated Reports
/// </summary>
public class SsrsReportService : IReportService
{
    private readonly string _reportServerUrl;

    public SsrsReportService(string reportServerUrl)
    {
        _reportServerUrl = reportServerUrl;
    }

    public async Task<byte[]> RenderReportAsync(string reportPath, Dictionary<string, string> parameters)
    {
        Console.WriteLine($"[{DateTime.Now}] Rendering SSRS report: {reportPath}");

        // Build SSRS render URL
        // Example: http://ssrs01.doa.local/ReportServer?/DOA Reports/Monthly Sales&Year=2024&Month=3&rs:Format=PDF
        var paramString = string.Join("&", parameters.Select(p => $"{p.Key}={p.Value}"));
        var renderUrl = $"{_reportServerUrl}?{reportPath}&{paramString}&rs:Format=PDF";

        using var httpClient = new HttpClient(new HttpClientHandler
        {
            UseDefaultCredentials = true   // ⚠️ Windows auth to SSRS
        });

        var response = await httpClient.GetAsync(renderUrl);
        response.EnsureSuccessStatusCode();

        var reportBytes = await response.Content.ReadAsByteArrayAsync();
        Console.WriteLine($"[{DateTime.Now}] SSRS report rendered: {reportPath} ({reportBytes.Length} bytes)");

        return reportBytes;
    }
}
