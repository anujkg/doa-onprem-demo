using Azure.Identity;
using Microsoft.Extensions.Logging;

namespace DOA.WebApp.Services;

public interface IReportService
{
    Task<byte[]> RenderReportAsync(string reportPath, Dictionary<string, string> parameters);
}

/// <summary>
/// Power BI Paginated Reports service — replaces on-prem SSRS.
/// Uses DefaultAzureCredential to authenticate to Power BI REST API.
/// Reports must be published to a Power BI workspace as Paginated Reports (.rdl).
/// </summary>
public class PowerBiReportService : IReportService
{
    private readonly string _workspaceId;
    private readonly ILogger<PowerBiReportService> _logger;
    private readonly DefaultAzureCredential _credential;

    public PowerBiReportService(string workspaceId, ILogger<PowerBiReportService> logger)
    {
        _workspaceId = workspaceId;
        _logger = logger;
        _credential = new DefaultAzureCredential();
    }

    public async Task<byte[]> RenderReportAsync(string reportPath, Dictionary<string, string> parameters)
    {
        _logger.LogInformation("Rendering Power BI report: {ReportPath}", reportPath);

        var tokenRequestContext = new Azure.Core.TokenRequestContext(
            new[] { "https://analysis.windows.net/powerbi/api/.default" });
        var accessToken = await _credential.GetTokenAsync(tokenRequestContext);

        var paramString = string.Join("&", parameters.Select(p =>
            $"rp:{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        var reportName = Uri.EscapeDataString(reportPath.TrimStart('/').Replace(" ", "%20"));
        var exportUrl = $"https://api.powerbi.com/v1.0/myorg/groups/{_workspaceId}/reports/{reportName}/ExportTo";

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken.Token);

        var requestBody = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(new
            {
                format = "PDF",
                paginatedReportConfiguration = new
                {
                    parameterValues = parameters.Select(p => new { name = p.Key, value = p.Value }).ToArray()
                }
            }),
            System.Text.Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(exportUrl, requestBody);
        response.EnsureSuccessStatusCode();

        var reportBytes = await response.Content.ReadAsByteArrayAsync();
        _logger.LogInformation("Power BI report rendered: {ReportPath} ({Bytes} bytes)", reportPath, reportBytes.Length);

        return reportBytes;
    }
}
