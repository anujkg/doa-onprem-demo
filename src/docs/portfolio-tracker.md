# Application Portfolio — Migration Tracker

> Track all applications through the migration pipeline.
> Update status after each wave completes.

## Portfolio Summary

| Metric | Count |
|--------|-------|
| Total Applications | 33 |
| Migrated ✅ | 1 |
| In Progress 🔄 | 0 |
| Planned 📋 | 12 |
| Not Started ⬜ | 20 |

---

## Wave Plan

### Wave 0 — Pilot ✅ Complete
| App | Pattern/Skill | Owner | Status | Duration | Notes |
|-----|--------------|-------|--------|----------|-------|
| DOA Order System | `onprem-k8s-to-container-apps` | Team Lead | ✅ Done | 1 session | Pilot app — skill validated |

### Wave 1 — Same Pattern (K8s → Container Apps)
| App | Pattern/Skill | Owner | Status | Duration | Notes |
|-----|--------------|-------|--------|----------|-------|
| Inventory Tracker | `onprem-k8s-to-container-apps` | Dev A | 📋 Planned | — | Same LDAP + SMTP deps |
| Customer Portal | `onprem-k8s-to-container-apps` | Dev A | 📋 Planned | — | Ping SSO + SSRS |
| Shipping Manager | `onprem-k8s-to-container-apps` | Dev B | 📋 Planned | — | CronJob + NFS share |
| Returns Processing | `onprem-k8s-to-container-apps` | Dev B | 📋 Planned | — | SQL injection issues |
| Vendor Gateway | `onprem-k8s-to-container-apps` | Dev C | 📋 Planned | — | External API integration |

### Wave 2 — VM-Hosted Apps
| App | Pattern/Skill | Owner | Status | Duration | Notes |
|-----|--------------|-------|--------|----------|-------|
| HR Benefits Portal | `vm-to-container-apps` | Dev A | 📋 Planned | — | IIS-hosted, .NET 6 |
| Compliance Reporter | `vm-to-container-apps` | Dev B | 📋 Planned | — | Heavy SSRS dependency |
| Audit Trail System | `vm-to-container-apps` | Dev C | 📋 Planned | — | Windows auth |

### Wave 3 — Windows Services
| App | Pattern/Skill | Owner | Status | Duration | Notes |
|-----|--------------|-------|--------|----------|-------|
| Nightly ETL Service | `windows-service-to-jobs` | Dev A | 📋 Planned | — | Task Scheduler → Jobs |
| Alert Monitor | `windows-service-to-jobs` | Dev B | 📋 Planned | — | Long-running process |
| Report Generator | `windows-service-to-jobs` | Dev C | 📋 Planned | — | PDF generation + email |
| Data Archiver | `windows-service-to-jobs` | Dev C | 📋 Planned | — | SQL + blob storage |

### Wave 4+ — Complex Migrations
| App | Pattern/Skill | Owner | Status | Duration | Notes |
|-----|--------------|-------|--------|----------|-------|
| Legacy WCF Gateway | `wcf-to-rest-api` | Senior Dev | ⬜ Not Started | — | Needs skill creation |
| Payment Processor | `wcf-to-rest-api` | Senior Dev | ⬜ Not Started | — | PCI compliance |
| ... | ... | ... | ⬜ Not Started | ... | ... |

---

## Skills Required vs. Available

| Skill | Status | Apps Using It |
|-------|--------|--------------|
| `onprem-k8s-to-container-apps` | ✅ Proven | 6 apps |
| `vm-to-container-apps` | 🔨 Build for Wave 2 | 8 apps |
| `windows-service-to-jobs` | 🔨 Build for Wave 3 | 6 apps |
| `wcf-to-rest-api` | 🔨 Build for Wave 4 | 4 apps |
| `shared/ldap-to-entra-id` | ✅ Embedded in K8s skill | Cross-cutting |
| `shared/smtp-to-acs` | ✅ Embedded in K8s skill | Cross-cutting |

---

## Migration Velocity Tracking

| Wave | Apps | Planned Duration | Actual Duration | Avg Per App |
|------|------|-----------------|-----------------|-------------|
| Wave 0 (Pilot) | 1 | 1 week | 1 session | ~2 hours |
| Wave 1 | 5 | 1 week | — | — |
| Wave 2 | 3 | 1 week | — | — |
| Wave 3 | 4 | 1 week | — | — |

> **Target:** After Wave 1, average migration time should be < 1 day per app (including review).
