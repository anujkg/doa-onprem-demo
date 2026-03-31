using DOA.BatchJobs.Jobs;
using Microsoft.Data.SqlClient;

// ===========================================================
// DOA Batch Job Runner
// Runs as a scheduled task via Windows Task Scheduler (on-prem)
// Triggered nightly at 2:00 AM by: schtasks
//
// ⚠️ MIGRATION TARGET: Azure Container Apps Job (cron schedule)
// ===========================================================

Console.WriteLine($"[{DateTime.Now}] ========================================");
Console.WriteLine($"[{DateTime.Now}] DOA Batch Job Runner — Starting");
Console.WriteLine($"[{DateTime.Now}] ========================================");

// ⚠️ Connection string hardcoded — reads from environment variable on prod server
var connectionString = Environment.GetEnvironmentVariable("DOA_SQL_CONNSTR")
    ?? "Server=sqlprod01.doa.local;Database=DOA_Orders;User Id=doa_batch;Password=B@tch!2024;TrustServerCertificate=true";

var smtpHost = Environment.GetEnvironmentVariable("DOA_SMTP_HOST") ?? "smtp.doa.local";

try
{
    // Job 1: Nightly Data Import — pull data from external feed
    Console.WriteLine($"[{DateTime.Now}] --- Job 1: Data Import ---");
    var importJob = new DataImportJob(connectionString);
    var importedCount = await importJob.RunAsync();
    Console.WriteLine($"[{DateTime.Now}] Imported {importedCount} records");

    // Job 2: Order Status Sync — sync with Mercury application
    Console.WriteLine($"[{DateTime.Now}] --- Job 2: Order Status Sync ---");
    var syncJob = new OrderSyncJob(connectionString);
    var syncedCount = await syncJob.RunAsync();
    Console.WriteLine($"[{DateTime.Now}] Synced {syncedCount} order statuses");

    // Job 3: Email Notifications — send pending email digests
    Console.WriteLine($"[{DateTime.Now}] --- Job 3: Email Notifications ---");
    var emailJob = new EmailDigestJob(connectionString, smtpHost);
    var emailsSent = await emailJob.RunAsync();
    Console.WriteLine($"[{DateTime.Now}] Sent {emailsSent} notification emails");

    // Job 4: Data Cleanup — archive orders older than 2 years
    Console.WriteLine($"[{DateTime.Now}] --- Job 4: Data Cleanup ---");
    var cleanupJob = new DataCleanupJob(connectionString);
    var archivedCount = await cleanupJob.RunAsync();
    Console.WriteLine($"[{DateTime.Now}] Archived {archivedCount} old orders");

    Console.WriteLine($"[{DateTime.Now}] ========================================");
    Console.WriteLine($"[{DateTime.Now}] All jobs completed successfully");
    Console.WriteLine($"[{DateTime.Now}] ========================================");
    Environment.ExitCode = 0;
}
catch (Exception ex)
{
    Console.WriteLine($"[{DateTime.Now}] ❌ BATCH JOB FAILED: {ex.Message}");
    Console.WriteLine($"[{DateTime.Now}] Stack: {ex.StackTrace}");
    Environment.ExitCode = 1;
}
