# DOA Order System — Demo Source (On-Premises)

> **Purpose:** This folder contains the "before" state of a realistic on-prem .NET application
> that will be migrated to Azure using GitHub Copilot Agent Mode + Custom Instructions + Skills.

## What This Represents

A customer (DOA) has shared their current on-prem architecture. They're running:

| Component | Technology | On-Prem Location |
|-----------|-----------|-----------------|
| Web API | .NET 8, K8s (3 replicas) | `harbor.doa.local` → on-prem K8s |
| Batch Jobs | .NET 8 Console App (K8s CronJob) | Nightly at 2 AM |
| Database | SQL Server (DBA-managed) | `sqlprod01.doa.local` |
| Auth (external) | Ping SSO (SAML) | Federated |
| Auth (internal) | Active Directory LDAP | `dc01.doa.local:389` |
| Email | On-prem SMTP relay | `smtp.doa.local:25` |
| Reporting | SSRS | `ssrs01.doa.local` |
| Load Balancer | F5 | VIP: 10.0.100.50 |
| CI/CD | GitHub Actions (self-hosted runner) | On-prem |
| Container Registry | Harbor | `harbor.doa.local` |

## Known Issues in This Code (Intentional for Demo)

These are **real anti-patterns** found in enterprise apps — the demo shows Copilot fixing them:

1. **SQL Injection** — `OrderRepository.GetOrdersAsync()` uses string concatenation
2. **SQL Injection** — `DataImportJob.RunAsync()` uses string interpolation in SQL
3. **Secrets in Config** — SQL passwords in `appsettings.json` and K8s env vars
4. **LDAP password in Config** — `Ldap:ServicePassword` in appsettings.json
5. **No Health Checks** — No `/healthz` endpoint, no K8s probes
6. **Running as Root** — No `securityContext` in K8s, no `USER` in Dockerfile
7. **Console.WriteLine Logging** — No structured logging anywhere
8. **SDK Image for Runtime** — Using `dotnet/sdk` instead of `dotnet/aspnet` as runtime
9. **SMTP without TLS** — Internal relay, no encryption
10. **NFS Dependency** — Batch job reads from network file share

## Folder Structure

```
demo/
├── .instructions.md              ← Custom Instructions (shown in demo)
├── skills/
│   └── onprem-k8s-to-container-apps/
│       └── SKILL.md              ← Migration Skill (shown in demo)
└── src/
    ├── DOA.WebApp/               ← The web API application
    │   ├── Program.cs            ← Main entry — LDAP, SMTP, SSRS wired up
    │   ├── appsettings.json      ← ⚠️ Contains secrets
    │   ├── Auth/
    │   │   ├── ILdapAuthService.cs
    │   │   └── LdapAuthService.cs   ← LDAP integration (migration target)
    │   ├── Controllers/
    │   │   ├── OrdersController.cs   ← Order CRUD + email notifications
    │   │   └── ReportsController.cs  ← SSRS report rendering
    │   └── Services/
    │       ├── OrderRepository.cs    ← SQL data access (has SQL injection!)
    │       ├── SmtpEmailService.cs   ← On-prem SMTP relay
    │       └── SsrsReportService.cs  ← SSRS integration
    ├── DOA.BatchJobs/             ← The scheduled batch processor
    │   ├── Program.cs            ← Job runner (triggered by K8s CronJob)
    │   └── Jobs/
    │       ├── DataImportJob.cs     ← CSV import from file share
    │       ├── OrderSyncJob.cs      ← Mercury API sync
    │       ├── EmailDigestJob.cs    ← Nightly email notifications
    │       └── DataCleanupJob.cs    ← Archive old orders
    ├── k8s/                       ← Kubernetes manifests (on-prem)
    │   ├── namespace.yaml
    │   ├── webapp-deployment.yaml   ← 3-replica deployment + NodePort
    │   └── batch-cronjob.yaml       ← Nightly CronJob + NFS mount
    ├── .github/workflows/
    │   └── ci-cd.yml              ← Current CI/CD (self-hosted → Harbor → K8s)
    ├── Dockerfile                 ← Web app (⚠️ single-stage, root user)
    ├── Dockerfile.batch           ← Batch jobs (⚠️ same issues)
    └── docs/
        └── architecture.md        ← Full architecture documentation
```

## How to Use in Demo

1. **Open this folder in VS Code** — the `.instructions.md` at the demo root will be auto-loaded by Copilot
2. **Show the source code first** — walk through `Program.cs`, `LdapAuthService.cs`, `appsettings.json` to show the on-prem dependencies
3. **Show the K8s manifests** — point out secrets in env vars, no health checks, root user
4. **Show the Skill file** — explain how it teaches Copilot the migration patterns
5. **Switch to Agent Mode** — start prompting for migration (see `levelup-1hour-plan.md` for prompts)

## Important Note

This source code is **intentionally insecure and anti-pattern-heavy**. It represents a typical
enterprise on-prem application. The purpose is to demonstrate how Copilot + Custom Instructions + Skills
can identify and fix these issues during migration.

**Do not deploy this code anywhere.** It contains hardcoded credentials (for demo purposes only).
