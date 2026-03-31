# DOA Application — On-Premises Architecture

## Current State Overview

This document describes the current on-premises architecture for the DOA Order Management System.

---

## System Components

### 1. Web Application (DOA.WebApp)
- **.NET 8** web API running in **on-prem Kubernetes** (3 replicas)
- Hosted in internal Harbor container registry (`harbor.doa.local`)
- Exposed via **F5 load balancer** (VIP: 10.0.100.50) → NodePort 30080
- No health check probes configured
- Running as root in containers

### 2. Batch Processing (DOA.BatchJobs)
- **.NET 8 Console App** — runs as a **K8s CronJob** (nightly at 2:00 AM)
- 4 jobs: Data Import, Order Sync, Email Digest, Data Cleanup
- Reads CSV from **network file share** (`\\fileserver01.doa.local\DataFeeds`)
- Previously ran as Windows Task Scheduler job (migrated to K8s CronJob in 2024)

### 3. Database
- **SQL Server** on-prem (DBaaS-style, managed by DBA team)
- Primary: `sqlprod01.doa.local` / DR: `sqldr01.doa.local`
- Database: `DOA_Orders`
- Authentication: SQL Server auth (username/password in connection strings)

### 4. Authentication & Authorization
- **External users:** Ping SSO (SAML federation)
- **Internal users:** Active Directory LDAP (`dc01.doa.local`, port 389)
- LDAP groups control authorization: `DOA\AppAdmins`, `DOA\ReportViewers`
- Service account for LDAP queries: `svc_ldap_reader`

### 5. Email / Notifications
- **On-prem SMTP relay** (`smtp.doa.local`, port 25)
- No TLS, Windows Integrated auth on internal network
- Used by both web app (order confirmations) and batch jobs (nightly digests)

### 6. Reporting
- **SSRS** (SQL Server Reporting Services) at `http://ssrs01.doa.local/ReportServer`
- Reports: Monthly Sales, Inventory Status
- Windows Integrated auth to SSRS server
- Reports rendered as PDF and served via the web API

### 7. External Integrations
- **Mercury Application** — REST API for order status sync (`https://mercury-api.partner.com`)
- **Data Feed Export** — CSV files on network share

### 8. CI/CD
- **GitHub Actions** with self-hosted runner on-prem
- Builds → pushes to Harbor → deploys to K8s via kubectl
- No infrastructure-as-code (manual K8s manifests)

---

## Infrastructure Diagram

```
                    ┌─────────────┐
                    │   Internet   │
                    └──────┬──────┘
                           │
                    ┌──────▼──────┐
                    │  Ping SSO   │ (External auth)
                    └──────┬──────┘
                           │
                    ┌──────▼──────┐
                    │ F5 Load     │ VIP: 10.0.100.50
                    │ Balancer    │
                    └──────┬──────┘
                           │
              ┌────────────▼────────────┐
              │   On-Prem Kubernetes     │
              │                          │
              │  ┌──────────────────┐   │
              │  │ DOA.WebApp (×3)  │   │  ← .NET 8 API
              │  │ NodePort: 30080  │   │
              │  └────────┬─────────┘   │
              │           │              │
              │  ┌────────▼─────────┐   │
              │  │DOA.BatchJobs     │   │  ← CronJob (2 AM)
              │  │ (CronJob)        │   │
              │  └──────────────────┘   │
              └──────────┬──────────────┘
                         │
         ┌───────────────┼───────────────┐
         │               │               │
   ┌─────▼─────┐  ┌─────▼─────┐  ┌─────▼──────┐
   │ SQL Server │  │   LDAP    │  │   SMTP     │
   │ (on-prem) │  │ AD DC01   │  │ Relay      │
   └───────────┘  └───────────┘  └────────────┘
         │
   ┌─────▼─────┐
   │   SSRS    │ (Reporting)
   └───────────┘

   External:
   ┌──────────────┐  ┌──────────────┐
   │ Mercury API  │  │ File Share   │
   │ (partner)    │  │ (NFS/SMB)    │
   └──────────────┘  └──────────────┘
```

---

## Known Issues / Technical Debt

| # | Issue | Severity |
|---|-------|----------|
| 1 | SQL credentials in connection strings (env vars) | 🔴 High |
| 2 | LDAP service password in appsettings.json | 🔴 High |
| 3 | SQL injection in DataImportJob (string concatenation) | 🔴 High |
| 4 | SQL injection in GetOrdersAsync (status filter) | 🔴 High |
| 5 | No health check probes on K8s deployment | 🟡 Medium |
| 6 | Running as root in all containers | 🟡 Medium |
| 7 | Using SDK image for runtime (bloated container) | 🟡 Medium |
| 8 | Console.WriteLine instead of structured logging | 🟡 Medium |
| 9 | SMTP with no TLS | 🟡 Medium |
| 10 | No network policies in K8s namespace | 🟡 Medium |
| 11 | NFS mount dependency for data import | 🟢 Low |
| 12 | No pod security policies / contexts | 🟢 Low |

---

## Migration Targets (Proposed)

| On-Prem Component | Azure Target |
|---|---|
| K8s Deployment (web) | Azure Container Apps |
| K8s CronJob (batch) | Azure Container Apps Jobs |
| SQL Server | Azure SQL Database |
| F5 Load Balancer | Azure Front Door |
| LDAP / Active Directory | Microsoft Entra ID + MSAL |
| Ping SSO | Entra ID External Identities |
| SMTP Relay | Azure Communication Services |
| SSRS Reports | Power BI Paginated Reports |
| Harbor Registry | Azure Container Registry (ACR) |
| File Share (NFS) | Azure Blob Storage |
| GitHub Actions (self-hosted) | GitHub Actions (GitHub-hosted) |
