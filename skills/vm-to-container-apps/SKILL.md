# SKILL.md — VM-Hosted .NET to Azure Container Apps Migration

## Description
Migrates .NET applications running on IIS/Windows VMs to Azure Container Apps.
Handles containerization (Dockerfile creation), IIS feature mapping, Windows Auth conversion,
infrastructure generation, and CI/CD setup for apps that were never containerized.

## When to Use
- Customer has .NET apps hosted on Windows VMs (IIS)
- Apps are NOT currently containerized — need Dockerfile creation from scratch
- Target is Azure Container Apps (serverless containers)
- Need to replace Windows-specific dependencies: Windows Auth, IIS URL Rewrite, SMTP, file system

## Pre-Conditions
- Source code is .NET 6+ (or can be upgraded first)
- Application runs on IIS with a `web.config` present
- No hard dependency on Windows kernel features (COM interop, registry, etc.)

---

## Migration Steps

### Step 1: Analyze Current Architecture
1. Read `web.config` to understand IIS configuration:
   - URL rewrite rules → Container Apps ingress routing
   - Authentication mode (Windows, Forms, None)
   - Connection strings and app settings
   - HTTP modules and handlers
2. Read application code to identify VM-specific dependencies:
   - `System.IO` file paths (`C:\`, `D:\`, UNC paths) → Azure Blob Storage
   - `System.DirectoryServices` → LDAP/Windows Auth dependency
   - `EventLog` → Structured logging replacement
   - `Registry` → App configuration / Key Vault
   - `System.Drawing` (GDI+) → Linux-compatible image library
3. Check for Windows-only NuGet packages
4. Produce a migration assessment with blockers

### Step 2: Create Dockerfile (From Scratch)
1. Create multi-stage `Dockerfile`:
   - Build stage: `mcr.microsoft.com/dotnet/sdk:8.0` 
   - Runtime stage: `mcr.microsoft.com/dotnet/aspnet:8.0` (Linux)
   - Non-root user: `USER app`
   - Health check: `HEALTHCHECK CMD curl -f http://localhost:8080/healthz || exit 1`
2. Create `.dockerignore` (exclude bin, obj, .vs, .git)
3. Verify the app builds in Linux container (no Windows-only APIs)

### Step 3: Generate Azure Infrastructure (Bicep)
1. Create `infra/main.bicep` with:
   - Container Apps Environment
   - Container App (with ingress, scaling, health probes)
   - Azure SQL Database (Managed Identity auth)
   - Azure Key Vault (secrets from web.config)
   - Azure Container Registry
   - Azure Front Door (if public-facing)
   - User-Assigned Managed Identity
   - Azure Blob Storage (if file system replacement needed)
2. Map IIS scaling (VM count) to Container Apps scaling rules

### Step 4: Migrate Authentication
- **Windows Auth → Entra ID**: Replace `[Authorize]` with MSAL + `[Authorize(Roles="...")]`
- **Forms Auth → Entra ID**: Remove FormsAuthentication, add `Microsoft.Identity.Web`
- **Anonymous**: No changes needed

### Step 5: Replace File System with Azure Blob Storage
1. Replace `System.IO.File` reads/writes with `Azure.Storage.Blobs`
2. Replace file path references (`C:\Data\`, UNC paths) with blob container URIs
3. Use `DefaultAzureCredential` for storage auth (no connection strings)

### Step 6: Replace Windows Event Log with Structured Logging
1. Remove `EventLog.WriteEntry()` calls
2. Add Serilog with Console + Application Insights sinks
3. Use structured logging: `Log.Information("Order {OrderId} processed", orderId)`

### Step 7: Map IIS Features to Container Apps
| IIS Feature | Container Apps Equivalent |
|---|---|
| URL Rewrite | Ingress routing rules / app-level middleware |
| Custom Error Pages | ASP.NET middleware `UseExceptionHandler` |
| IP Restrictions | Container Apps IP restrictions |
| SSL/TLS | Built-in TLS termination |
| CORS | ASP.NET CORS middleware |
| gzip Compression | ASP.NET response compression middleware |
| Request Filtering | ASP.NET request size limits |

### Step 8: Modernize CI/CD
1. Create GitHub Actions workflow:
   - Build → Docker build → Push to ACR → Deploy to Container Apps
   - Azure OIDC login (no stored credentials)
   - Health check verification after deploy

---

## Component Mapping Rules

| VM Component | Azure Equivalent | Complexity |
|---|---|---|
| IIS on Windows VM | Container Apps | 🟡 Medium — containerization |
| Windows Task Scheduler | Container Apps Jobs | 🟢 Low — cron trigger |
| Windows Auth (IIS) | Microsoft Entra ID | 🔴 High — code changes |
| Forms Auth | Microsoft Entra ID | 🟡 Medium — MSAL swap |
| Event Log | Serilog + App Insights | 🟢 Low — logging swap |
| Local file system (C:\) | Azure Blob Storage | 🟡 Medium — SDK replacement |
| web.config settings | Azure Key Vault + App Settings | 🟢 Low — config migration |
| IIS URL Rewrite | ASP.NET middleware | 🟡 Medium — rule conversion |
| SMTP (IIS pickup) | Azure Communication Services | 🟡 Medium — SDK replacement |
| SQL Server (on-prem) | Azure SQL Database | 🟡 Medium — auth change |

## Security Fixes to Apply During Migration

| Vulnerability | Fix |
|---|---|
| Secrets in web.config | Azure Key Vault references |
| SQL auth with password | Managed Identity + DefaultAzureCredential |
| Windows Auth pass-through | Entra ID with explicit claims |
| No containerization | Dockerfile with non-root user |
| Event Log (local only) | Centralized logging (App Insights) |
| No health monitoring | `/healthz` endpoint + Container Apps probes |
