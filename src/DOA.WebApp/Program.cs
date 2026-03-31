using DOA.WebApp.Auth;
using DOA.WebApp.Services;
using Microsoft.AspNetCore.Authentication.Negotiate;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// Authentication — LDAP / Active Directory (On-Prem)
// Uses Windows Negotiate (Kerberos/NTLM) + LDAP group lookups
// ============================================================
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("DOA\\AppAdmins"));
    options.AddPolicy("ReportViewer", policy =>
        policy.RequireRole("DOA\\ReportViewers"));
});

// ============================================================
// LDAP Service — queries Active Directory for user details
// ============================================================
builder.Services.AddSingleton<ILdapAuthService>(sp =>
{
    var config = builder.Configuration;
    return new LdapAuthService(
        ldapServer: config["Ldap:Server"]!,        // e.g. "ldap://dc01.doa.local"
        ldapPort: int.Parse(config["Ldap:Port"]!),  // 389
        baseDn: config["Ldap:BaseDN"]!,             // "DC=doa,DC=local"
        serviceAccount: config["Ldap:ServiceAccount"]!,
        servicePassword: config["Ldap:ServicePassword"]!  // ⚠️ Password in config
    );
});

// ============================================================
// Data Access — SQL Server with raw connection string
// ============================================================
builder.Services.AddScoped<IOrderRepository>(sp =>
{
    // ⚠️ Connection string has username/password embedded
    var connStr = builder.Configuration.GetConnectionString("SqlServer")!;
    return new OrderRepository(connStr);
});

// ============================================================
// Email Notifications — On-prem SMTP relay
// ============================================================
builder.Services.AddSingleton<IEmailService>(sp =>
{
    var config = builder.Configuration;
    return new SmtpEmailService(
        smtpHost: config["Smtp:Host"]!,       // "smtp.doa.local"
        smtpPort: int.Parse(config["Smtp:Port"]!),  // 25
        fromAddress: config["Smtp:FromAddress"]!
    );
});

// ============================================================
// Reporting — SSRS integration
// ============================================================
builder.Services.AddSingleton<IReportService>(sp =>
{
    var reportServerUrl = builder.Configuration["Ssrs:ServerUrl"]!;
    return new SsrsReportService(reportServerUrl);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Logging — basic Console.WriteLine pattern (no structured logging)
Console.WriteLine($"[{DateTime.Now}] DOA WebApp starting...");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine($"[{DateTime.Now}] DOA WebApp started on {string.Join(", ", app.Urls)}");
app.Run();
