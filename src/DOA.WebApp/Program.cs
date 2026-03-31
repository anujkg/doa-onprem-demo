using Azure.Identity;
using DOA.WebApp.Services;
using Microsoft.Identity.Web;
using Serilog;

// Configure Serilog early
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ============================================================
    // Serilog structured logging
    // ============================================================
    builder.Host.UseSerilog((ctx, services, config) =>
        config
            .ReadFrom.Configuration(ctx.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"));

    // ============================================================
    // Key Vault configuration (secrets loaded at startup)
    // ============================================================
    var keyVaultEndpoint = builder.Configuration["KeyVault:Endpoint"];
    if (!string.IsNullOrEmpty(keyVaultEndpoint))
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultEndpoint),
            new DefaultAzureCredential());
    }

    // ============================================================
    // Authentication — Microsoft Entra ID (replacing LDAP/Negotiate)
    // ============================================================
    builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "AzureAd");

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy =>
            policy.RequireRole("AppAdmins"));
        options.AddPolicy("ReportViewer", policy =>
            policy.RequireRole("ReportViewers"));
    });

    // ============================================================
    // Data Access — Azure SQL with Managed Identity
    // ============================================================
    builder.Services.AddScoped<IOrderRepository>(sp =>
    {
        var connStr = builder.Configuration.GetConnectionString("SqlServer")!;
        var logger = sp.GetRequiredService<ILogger<OrderRepository>>();
        return new OrderRepository(connStr, logger);
    });

    // ============================================================
    // Email — Azure Communication Services (replacing on-prem SMTP)
    // ============================================================
    builder.Services.AddSingleton<IEmailService>(sp =>
    {
        var config = builder.Configuration;
        var logger = sp.GetRequiredService<ILogger<AzureCommunicationEmailService>>();
        return new AzureCommunicationEmailService(
            acsEndpoint: config["AzureCommunicationServices:Endpoint"]!,
            fromAddress: config["AzureCommunicationServices:FromAddress"]!,
            logger: logger);
    });

    // ============================================================
    // Reporting — Power BI Paginated Reports (replacing SSRS)
    // ============================================================
    builder.Services.AddSingleton<IReportService>(sp =>
    {
        var workspaceId = builder.Configuration["PowerBi:WorkspaceId"]!;
        var logger = sp.GetRequiredService<ILogger<PowerBiReportService>>();
        return new PowerBiReportService(workspaceId, logger);
    });

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // ============================================================
    // Health checks (required for Container Apps probes)
    // ============================================================
    builder.Services.AddHealthChecks();

    Log.Information("DOA WebApp starting...");

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseSerilogRequestLogging();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/healthz");

    Log.Information("DOA WebApp started");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "DOA WebApp failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
