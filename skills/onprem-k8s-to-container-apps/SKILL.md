# SKILL.md — On-Prem Kubernetes to Azure Container Apps Migration

## Description
Migrates .NET applications from on-premises Kubernetes clusters to Azure Container Apps.
Handles the full migration scope: infrastructure generation, auth conversion (LDAP → Entra ID),
service replacement (SMTP → Azure Communication Services, SSRS → Power BI), 
batch job conversion (K8s CronJob → Container Apps Jobs), and CI/CD modernization.

## When to Use
- Customer has .NET apps running on on-prem Kubernetes
- Target is Azure Container Apps (NOT AKS) — serverless container platform
- Need to replace on-prem dependencies: LDAP, SMTP, SSRS, F5, Harbor, NFS file shares
- Need to generate Bicep IaC and modernize GitHub Actions pipelines

## Pre-Conditions
- Source code is .NET 8 (or can be upgraded)
- Existing K8s manifests available for reference
- GitHub Actions already in use (self-hosted runners)

---

## Migration Steps

### Step 1: Analyze Current Architecture
1. Read all K8s manifests (`k8s/*.yaml`) to understand deployments, services, cron jobs
2. Read the application code to identify on-prem dependencies:
   - `System.DirectoryServices` → LDAP dependency
   - `SmtpClient` → SMTP dependency
   - SSRS URL references → Reporting dependency
   - SQL connection strings with passwords → Database auth dependency
   - NFS/SMB paths → File storage dependency
3. Read the existing CI/CD pipeline (`.github/workflows/`)
4. Produce a component dependency map with migration targets

### Step 2: Generate Azure Infrastructure (Bicep)
1. Create `infra/main.bicep` with:
   - Container Apps Environment
   - Container App for web workload (with ingress, scaling rules)
   - Container Apps Job for batch workload (with cron schedule from K8s CronJob)
   - Azure SQL Database (with Managed Identity auth)
   - Azure Key Vault (for secrets)
   - Azure Container Registry
   - Azure Front Door profile + endpoint
   - Azure Communication Services (for email)
   - User-Assigned Managed Identity (shared by all resources)
2. Create `infra/parameters.prod.bicepparam` for production values
3. Follow naming conventions from Custom Instructions

### Step 3: Fix Dockerfiles
1. Convert to multi-stage builds (build with SDK, run with ASP.NET runtime)
2. Add non-root user (`USER app` or `USER 1000`)
3. Add `HEALTHCHECK` instruction
4. Create proper `.dockerignore`

### Step 4: Migrate Authentication (LDAP → Entra ID)
1. Remove NuGet packages: `System.DirectoryServices`, `System.DirectoryServices.AccountManagement`
2. Add NuGet packages: `Microsoft.Identity.Web`, `Microsoft.Identity.Web.UI`
3. Replace `LdapAuthService` with Entra ID configuration:
   - `builder.Services.AddMicrosoftIdentityWebApiAuthentication(Configuration)`
   - Configure `AzureAd` section in appsettings (TenantId, ClientId)
4. Map LDAP groups to Entra ID App Roles or Security Groups
5. Update authorization policies to use Entra ID claims

### Step 5: Replace SMTP with Azure Communication Services
1. Remove `System.Net.Mail` / `SmtpClient` usage
2. Add NuGet package: `Azure.Communication.Email`
3. Replace `SmtpEmailService` with `AzureCommunicationEmailService`
4. Use `EmailClient` with `DefaultAzureCredential` (Managed Identity)

### Step 6: Convert Batch Jobs to Container Apps Jobs
1. Add Serilog and structured logging (replace `Console.WriteLine`)
2. Add health/status reporting
3. Replace NFS file share paths with Azure Blob Storage SDK
4. Replace SQL connection string auth with `DefaultAzureCredential` + `SqlConnection`
5. Fix SQL injection vulnerabilities (parameterized queries)
6. Bicep: Create `Microsoft.App/jobs` resource with cron trigger matching original schedule

### Step 7: Modernize CI/CD (GitHub Actions)
1. Replace self-hosted runner with GitHub-hosted (`ubuntu-latest`)
2. Replace Harbor login with ACR login (`azure/docker-login@v2`)
3. Add Azure OIDC login step (`azure/login@v2` with federated credentials)
4. Build and push to ACR
5. Deploy with `az containerapp update` or `az containerapp job start`
6. Add health check verification step after deployment

---

## Component Mapping Rules

| On-Prem Component | Azure Equivalent | Migration Complexity |
|---|---|---|
| K8s Deployment | Container Apps App | 🟢 Low — Bicep generation |
| K8s CronJob | Container Apps Job (scheduled trigger) | 🟢 Low — Bicep + cron expression |
| K8s Service (ClusterIP) | Container Apps built-in service discovery | 🟢 Low — automatic |
| K8s NodePort + F5 | Azure Front Door + Container Apps ingress | 🟡 Medium — networking config |
| Harbor Registry | Azure Container Registry | 🟢 Low — image push target change |
| LDAP / Active Directory | Microsoft Entra ID + MSAL | 🔴 High — code changes required |
| Ping SSO (SAML) | Entra ID External Identities | 🟡 Medium — federation config |
| SMTP Relay (on-prem) | Azure Communication Services (Email) | 🟡 Medium — SDK replacement |
| SSRS Reports | Power BI Paginated Reports | 🔴 High — report migration |
| SQL Server (on-prem) | Azure SQL Database | 🟡 Medium — auth model change |
| NFS File Share | Azure Blob Storage | 🟡 Medium — SDK replacement |
| GitHub Actions (self-hosted) | GitHub Actions (GitHub-hosted + OIDC) | 🟢 Low — workflow update |

## Security Fixes to Apply During Migration

| Vulnerability | Fix |
|---|---|
| SQL injection (string concatenation) | Parameterized queries with `@param` syntax |
| Secrets in env vars / appsettings | Azure Key Vault + Key Vault provider |
| SQL auth with password | Managed Identity + DefaultAzureCredential |
| SMTP without TLS | Azure Communication Services (TLS by default) |
| Containers running as root | `USER app` in Dockerfile + securityContext |
| No health probes | `/healthz` endpoint + K8s/Container Apps probes |

## NuGet Package Changes

| Remove | Add |
|---|---|
| System.DirectoryServices | Microsoft.Identity.Web |
| System.DirectoryServices.AccountManagement | Microsoft.Identity.Web.UI |
| — | Azure.Communication.Email |
| — | Azure.Identity |
| — | Azure.Storage.Blobs |
| — | Azure.Security.KeyVault.Secrets |
| — | Serilog.AspNetCore |
| — | Serilog.Sinks.Console |
| — | Azure.Extensions.AspNetCore.Configuration.Secrets |
| — | Microsoft.Extensions.Diagnostics.HealthChecks |
