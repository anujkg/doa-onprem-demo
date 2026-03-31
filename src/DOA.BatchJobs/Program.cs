using Azure.Identity;
using Azure.Storage.Blobs;
using DOA.BatchJobs.Jobs;
using Serilog;

// Configure Serilog structured logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

Log.Information("========================================");
Log.Information("DOA Batch Job Runner — Starting");
Log.Information("========================================");

// Azure SQL — uses Managed Identity (DefaultAzureCredential), no passwords
var connectionString = Environment.GetEnvironmentVariable("DOA_SQL_CONNSTR")
    ?? "Server=sql-doa-prod.database.windows.net;Database=DOA_Orders;Authentication=Active Directory Managed Identity;Encrypt=true";

// Azure Communication Services endpoint (from Key Vault via env var)
var acsEndpoint = Environment.GetEnvironmentVariable("DOA_ACS_ENDPOINT")
    ?? "https://<acs-resource>.communication.azure.com";

var acsFromAddress = Environment.GetEnvironmentVariable("DOA_ACS_FROM_ADDRESS")
    ?? "DoNotReply@<acs-domain>.azurecomm.net";

// Azure Blob Storage for data feeds (replacing NFS file share)
var blobConnectionString = Environment.GetEnvironmentVariable("DOA_BLOB_ACCOUNT_URL")
    ?? "https://<storage-account>.blob.core.windows.net";

var credential = new DefaultAzureCredential();
var blobServiceClient = new BlobServiceClient(new Uri(blobConnectionString), credential);

try
{
    // Job 1: Nightly Data Import — pull data from Azure Blob Storage
    Log.Information("--- Job 1: Data Import ---");
    var importJob = new DataImportJob(connectionString, blobServiceClient);
    var importedCount = await importJob.RunAsync();
    Log.Information("Imported {Count} records", importedCount);

    // Job 2: Order Status Sync — sync with Mercury application
    Log.Information("--- Job 2: Order Status Sync ---");
    var syncJob = new OrderSyncJob(connectionString);
    var syncedCount = await syncJob.RunAsync();
    Log.Information("Synced {Count} order statuses", syncedCount);

    // Job 3: Email Notifications — send pending email digests via ACS
    Log.Information("--- Job 3: Email Notifications ---");
    var emailJob = new EmailDigestJob(connectionString, acsEndpoint, acsFromAddress, credential);
    var emailsSent = await emailJob.RunAsync();
    Log.Information("Sent {Count} notification emails", emailsSent);

    // Job 4: Data Cleanup — archive orders older than 2 years
    Log.Information("--- Job 4: Data Cleanup ---");
    var cleanupJob = new DataCleanupJob(connectionString);
    var archivedCount = await cleanupJob.RunAsync();
    Log.Information("Archived {Count} old orders", archivedCount);

    Log.Information("========================================");
    Log.Information("All jobs completed successfully");
    Log.Information("========================================");
    Environment.ExitCode = 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "BATCH JOB FAILED");
    Environment.ExitCode = 1;
}
finally
{
    Log.CloseAndFlush();
}
