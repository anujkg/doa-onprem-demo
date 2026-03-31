# Agentic Application Modernization at Scale

## The Core Idea

Traditional migration: **1 app × 1 team × months of effort = linear scaling**
Agentic migration: **1 skill × Copilot Coding Agent × N apps in parallel = no human bottleneck**

> **Key insight:** Scale doesn't mean "more developers going faster."
> It means **assign 10 GitHub Issues to Copilot, go to lunch, come back to 10 PRs.**
> No developer sits at VS Code. The agent works autonomously.

---

## How It Works — The Skill-Based Approach

### What We Just Demonstrated (1 App)

We migrated **DOA Order System** from on-prem Kubernetes → Azure Container Apps using:

| Asset | Purpose |
|-------|---------|
| **Custom Instructions** (`.instructions.md`) | Org-wide standards — naming, security, Bicep conventions |
| **Migration Skill** (`SKILL.md`) | Step-by-step migration playbook — component mapping, code transforms, IaC generation |
| **GitHub Copilot Agent Mode** | AI agent that executes the skill against real source code |

**Result:** Full migration in a single session — infrastructure, auth, services, security fixes, CI/CD.

### What Changes for the Next 50 Apps?

**Nothing.** The skill is reusable. You don't even need developers sitting at keyboards.
GitHub Copilot has a feature called **Copilot Coding Agent** that does this autonomously.

---

## GitHub Copilot Features That Enable Scale

### Feature 1: Copilot Coding Agent (The Parallel Engine)

**What it is:** You assign a GitHub Issue to `@copilot`, and it autonomously:
- Creates a branch
- Reads the codebase + skills + custom instructions
- Makes all the code changes
- Opens a Pull Request
- **No human at a keyboard required**

**Why it enables scale:** You can assign **multiple issues across multiple repos simultaneously**.
Copilot works on all of them in parallel, in the cloud, not on anyone's laptop.

```
┌─────────────────────────────────────────────────────────────────┐
│  GitHub Issues — All assigned to @copilot at 9:00 AM            │
│                                                                 │
│  repo: inventory-tracker                                        │
│  Issue #42: "Migrate to Azure Container Apps using the          │
│              onprem-k8s-to-container-apps skill"                │
│  Assignee: @copilot  ───→  🤖 Working...  ───→  PR #43 ✅     │
│                                                                 │
│  repo: customer-portal                                          │
│  Issue #18: "Migrate to Azure Container Apps using the          │
│              onprem-k8s-to-container-apps skill"                │
│  Assignee: @copilot  ───→  🤖 Working...  ───→  PR #19 ✅     │
│                                                                 │
│  repo: shipping-manager                                         │
│  Issue #7:  "Migrate to Azure Container Apps using the          │
│              onprem-k8s-to-container-apps skill"                │
│  Assignee: @copilot  ───→  🤖 Working...  ───→  PR #8 ✅      │
│                                                                 │
│  repo: returns-processing                                       │
│  Issue #31: "Migrate to Azure Container Apps using the          │
│              onprem-k8s-to-container-apps skill"                │
│  Assignee: @copilot  ───→  🤖 Working...  ───→  PR #32 ✅     │
│                                                                 │
│  repo: vendor-gateway                                           │
│  Issue #12: "Migrate to Azure Container Apps using the          │
│              onprem-k8s-to-container-apps skill"                │
│  Assignee: @copilot  ───→  🤖 Working...  ───→  PR #13 ✅     │
│                                                                 │
│  By 11:00 AM → 5 PRs ready for human review                    │
└─────────────────────────────────────────────────────────────────┘
```

### Feature 2: Custom Instructions (`.github/copilot-instructions.md`)

**What it is:** A markdown file committed to each repo that tells Copilot your org standards.
**Why it enables scale:** Every repo gets the same instructions — naming conventions, security
requirements, Bicep patterns. Copilot Coding Agent reads these automatically.

**Sharing strategy:** Put the file in an **org-level `.github` repo** or copy it into every app repo.

### Feature 3: Reusable Skills (SKILL.md + `copilot-skills.json`)

**What it is:** A structured migration playbook that the agent follows step-by-step.
**Why it enables scale:** The skill captures the migration pattern once. Whether it's
the Coding Agent working autonomously or a developer in VS Code Agent Mode, the same
skill produces the same consistent output.

**Sharing strategy:** Skills live in each repo's `.github/skills/` folder, or in a shared
org-level skills repo referenced by `copilot-skills.json`.

### Feature 4: Copilot Agent Mode (VS Code — Interactive)

**What it is:** The interactive version — developer in VS Code, Copilot executes the skill
with the developer reviewing in real-time.
**When to use:** Complex apps (🔴 High complexity) where human judgment is needed mid-migration.
Not needed for routine migrations — use Coding Agent for those.

---

## Two Modes of Parallel Execution

| Mode | How It Works | Best For | Human Effort |
|------|-------------|----------|--------------|
| **Copilot Coding Agent** | Assign Issues → Agent creates PRs autonomously | 🟢 Low / 🟡 Medium complexity apps | Review PRs only |
| **VS Code Agent Mode** | Developer invokes skill interactively | 🔴 High complexity apps | Developer guides the migration |

### The Scale Play: Use Both

```
30 apps in portfolio:
├── 20 apps (Low/Medium) ──→ Copilot Coding Agent (batch of Issues)
│                             Human role: review PRs, run tests
│                             Timeline: 1-2 days for all 20
│
└── 10 apps (High)       ──→ VS Code Agent Mode (developer-guided)
                              Human role: guide migration, handle edge cases
                              Timeline: 1 day each, 5 devs = 2 days
```

**Total: 30 apps migrated in under 1 week.**

---

## Scale Migration Framework

### Phase 1: Prove (Week 1-2) ✅ Done
- Migrate **1 pilot app** (DOA Order System)
- Validate the skill against real code
- Measure: time, accuracy, issues found
- **Output:** Proven skill + migration playbook

### Phase 2: Classify Portfolio (Week 3)
- Inventory all applications in the migration scope
- Classify each app by **migration pattern** (which skill applies)
- Group into waves by complexity and dependency

**Classification Matrix:**

| Pattern | Skill | Apps Matching | Complexity |
|---------|-------|---------------|------------|
| On-Prem K8s → Container Apps | `onprem-k8s-to-container-apps` | 12 apps | 🟡 Medium |
| VM-Hosted .NET → Container Apps | `vm-to-container-apps` | 8 apps | 🟡 Medium |
| Windows Service → Container Apps Jobs | `windows-service-to-container-jobs` | 6 apps | 🟢 Low |
| IIS + WCF → Container Apps | `wcf-to-container-apps` | 4 apps | 🔴 High |
| On-Prem K8s → AKS | `onprem-k8s-to-aks` | 3 apps | 🟢 Low |

### Phase 3: Build Skill Library (Week 3-4)
- Create one skill per migration pattern
- Each skill captures: steps, component mapping, code transforms, security fixes
- Skills are **version-controlled** and **reviewed** like code

### Phase 4: Execute in Parallel (Week 5+)

**This is the scale moment.** Use Copilot Coding Agent for batch execution.

#### Step 1: Create Migration Issues (5 minutes)
Create a GitHub Issue in each app repo with a standardized title and body:

```markdown
Title: Migrate to Azure Container Apps

Body:
Migrate this application from on-prem Kubernetes to Azure Container Apps
using the onprem-k8s-to-container-apps skill.

Follow the skill steps:
1. Analyze current architecture
2. Generate Bicep infrastructure
3. Fix Dockerfiles
4. Migrate LDAP auth to Entra ID
5. Replace SMTP with Azure Communication Services
6. Convert batch jobs to Container Apps Jobs
7. Apply all security fixes from the skill

Use the custom instructions in .github/copilot-instructions.md for
naming conventions and org standards.
```

#### Step 2: Assign All Issues to @copilot (1 minute)
Assign `@copilot` on all issues. Copilot Coding Agent starts working on all of them simultaneously.

#### Step 3: Wait for PRs (1-2 hours)
Copilot creates a branch, makes all changes, opens a PR for each app. **No developer is blocked.**

#### Step 4: Human Review (1 day)
Developers review the PRs. Because the skill enforces consistency, reviews are fast —
you're checking the same patterns you've already validated in the pilot.

```
┌─────────────────────────────────────────────────────────────────────┐
│  MONDAY MORNING — Batch Issue Assignment                            │
│                                                                     │
│  9:00 AM  Create 10 Issues across 10 repos                         │
│  9:05 AM  Assign all to @copilot                                    │
│  9:06 AM  Go do other work / grab coffee                            │
│                                                                     │
│  11:00 AM — 10 PRs waiting for review                               │
│                                                                     │
│  Dev A reviews: PR #43, PR #19, PR #8                               │
│  Dev B reviews: PR #32, PR #13, PR #27                              │
│  Dev C reviews: PR #51, PR #44, PR #16, PR #9                       │
│                                                                     │
│  By EOD: 10 apps migrated, reviewed, merged                         │
├─────────────────────────────────────────────────────────────────────┤
│  TUESDAY — Handle the complex ones (VS Code Agent Mode)             │
│                                                                     │
│  Dev A: WCF Gateway (interactive, needs human judgment)             │
│  Dev B: Payment Processor (PCI compliance, manual review)           │
│                                                                     │
│  Result: 2 complex apps done with developer guidance                │
├─────────────────────────────────────────────────────────────────────┤
│  WEDNESDAY — Next batch of 10 via Coding Agent                      │
│                                                                     │
│  Repeat. Assign 10 more Issues. Review PRs. Merge.                  │
└─────────────────────────────────────────────────────────────────────┘
```

**One week = 20-25 apps migrated, reviewed, and merged.**

### How to Set Up for Parallel Execution

#### Prerequisites
1. **GitHub Copilot Enterprise** license (required for Coding Agent)
2. **Copilot Coding Agent enabled** in org settings (Settings → Copilot → Coding Agent)
3. **Custom instructions** committed to each repo at `.github/copilot-instructions.md`
4. **Skills** committed to each repo at `.github/skills/` or in a shared org repo

#### Sharing Assets Across Repos

**Option A: Copy into each repo** (simplest)
```
each-app-repo/
├── .github/
│   ├── copilot-instructions.md       ← Org standards
│   └── skills/
│       └── onprem-k8s-to-container-apps/
│           └── SKILL.md              ← Migration skill
├── src/
│   └── ... (app code)
```

**Option B: Org-level `.github` repo** (DRY — write once, all repos inherit)
```
org/.github/                        ← Special repo, applies to all repos in org
├── copilot-instructions.md         ← Every repo gets these instructions
└── skills/
    ├── onprem-k8s-to-container-apps/
    │   └── SKILL.md
    └── vm-to-container-apps/
        └── SKILL.md
```

**Option C: Template repo with GitHub Actions automation**
- Create a migration-templates repo with all skills
- GitHub Action copies skills into target repos before Coding Agent runs

### What the Developer Experience Looks Like

**For Coding Agent (batch/routine migrations):**

```
1. PM creates Issue: "Migrate to Azure Container Apps using k8s skill"
2. PM assigns Issue to @copilot
3. Copilot Coding Agent:
   - Creates branch
   - Reads .github/copilot-instructions.md + skills/
   - Analyzes the codebase
   - Generates infrastructure, migrates code, fixes security
   - Opens Pull Request with summary of changes
4. Developer reviews PR, runs tests, merges
```

**For VS Code Agent Mode (complex/interactive migrations):**

```
1. Developer opens app in VS Code
2. Opens Copilot Agent Mode
3. Types: "Migrate this app to Azure Container Apps using the k8s skill"
4. Agent works step-by-step, developer reviews interactively
5. Developer creates PR when satisfied
```

**No architecture meetings. No design docs. No "let me figure out how LDAP migration works."**
The skill already knows.

---

## Why This Scales — The Math

### Traditional: Sequential + Manual
- 1 architect designs migration for each app
- Developers wait for architecture decisions
- Each app is a fresh effort

| Metric | Per App | 30 Apps (1 dev) | 30 Apps (5 devs, sequential) |
|--------|---------|-----------------|------------------------------|
| Assessment | 2-3 days | 60-90 days | 12-18 days |
| Infrastructure | 3-5 days | 90-150 days | 18-30 days |
| Code migration | 5-10 days | 150-300 days | 30-60 days |
| Security review | 2-3 days | 60-90 days | 12-18 days |
| **Total** | **12-21 days** | **360-630 days** | **72-126 days** |

Even with 5 developers, traditional takes **3-6 months** because each developer figures things out independently.

### Agentic: Parallel + Skill-Driven + Coding Agent

| Metric | Per App | 30 Apps (Coding Agent + 3 reviewers) |
|--------|---------|--------------------------------------|
| Coding Agent creates PR | ~30-60 min (autonomous) | All 30 run in parallel = **~1 hour** |
| Human review & testing | 2-4 hours | 10 apps/reviewer × 3 reviewers = **2-3 days** |
| **Total** | **½ day** | **3-4 days** |

### The Comparison

```
Traditional (5 devs):    ████████████████████████████████████████  3-6 months
Agentic (Coding Agent):  ██                                        3-4 days

                         Why? Coding Agent runs ALL migrations in parallel.
                         Developers only review PRs — they don't write code.
                         The skill eliminates the "figure it out" phase entirely.
```

### Three Dimensions of Acceleration

| Dimension | Traditional | Agentic | Improvement |
|-----------|------------|---------|-------------|
| **Speed per app** | 2-3 weeks | 30-60 min (agent) + 2-4 hr (review) | 15-20x |
| **Parallelism** | Limited (architect bottleneck) | Unlimited (Coding Agent runs in cloud) | No ceiling |
| **Human role** | Write code, design infra, fix security | Review PRs, run tests | 80% less effort |
| **Consistency** | Varies by developer | Identical (skill-enforced) | Eliminates rework |
| **Combined** | 3-6 months for 30 apps | 3-4 days for 30 apps | **~30-50x** |

---

## What Makes Skills Powerful at Scale

### 1. Consistency
Every app gets the same security fixes, the same Managed Identity setup, the same health check patterns. No "this developer forgot to remove SQL injection" variance.

### 2. Institutional Knowledge Capture
The skill encodes your **best architect's decisions** — component mapping, NuGet choices, Bicep patterns. Junior developers get senior-level output.

### 3. Incremental Improvement
After each wave, update the skill with lessons learned. Wave 2 is better than Wave 1. The skill gets smarter over time.

### 4. Auditability
The skill is a **versioned document**. You can show exactly what migration pattern was applied, when, and what it did. Compliance teams love this.

### 5. Composability
Skills can reference other skills. A complex migration might combine:
- `onprem-k8s-to-container-apps` (infrastructure)
- `ldap-to-entra-id` (auth)
- `smtp-to-communication-services` (email)

---

## Skill Library — Building Your Catalog

Each migration pattern becomes a skill:

```
skills/
├── onprem-k8s-to-container-apps/    ← Proven (DOA pilot)
│   └── SKILL.md
├── vm-to-container-apps/            ← Build for Wave 2
│   └── SKILL.md
├── windows-service-to-jobs/         ← Build for Wave 3
│   └── SKILL.md
├── wcf-to-rest-api/                 ← Build for Wave 4
│   └── SKILL.md
├── iis-to-app-service/              ← Cross-cutting
│   └── SKILL.md
└── shared/
    ├── ldap-to-entra-id/            ← Reusable auth skill
    │   └── SKILL.md
    ├── smtp-to-acs/                 ← Reusable email skill
    │   └── SKILL.md
    └── sql-auth-to-managed-identity/
        └── SKILL.md
```

---

## Customer Presentation Flow

### Slide 1: "Here's your DOA app — migrated"
Show the before/after. Highlight security fixes found automatically.

### Slide 2: "This is the skill that did it"
Show SKILL.md — it's readable, auditable, version-controlled.

### Slide 3: "GitHub Copilot has two modes for this"
| Mode | What Happens | Human Role |
|------|-------------|------------|
| **VS Code Agent Mode** | Developer invokes skill interactively | Guide + review in real-time |
| **Copilot Coding Agent** | Assign Issue to @copilot → PR appears | Review PR only |

The first mode is what we just demoed. The second mode is how you scale.

### Slide 4: "Coding Agent = assign 10 Issues, get 10 PRs"
Show the GitHub Issues → @copilot assignment → PRs flow diagram.
**This is the "aha" moment.** No developer at a keyboard. Copilot works autonomously in the cloud.

### Slide 5: "Your portfolio has 33 apps — they fall into 4-5 patterns"
Show the classification matrix. Most apps share patterns → same skill applies.

### Slide 6: "The workflow: Issue → @copilot → PR → Review → Merge"
Show the batch execution cadence. Monday: 10 issues assigned. By lunch: 10 PRs ready.

### Slide 7: "The math: 6 months → 1-2 weeks"
- Traditional: architect bottleneck, each app is a fresh effort
- Agentic: skill removes bottleneck, Coding Agent runs in parallel
- **Developers go from "writing migration code" to "reviewing migration PRs"**

### Slide 8: "Every app gets the same security hardening"
Show the security fixes table from the skill. No variance. Compliance-friendly.

### Slide 9: "Let's assign 3 Issues right now"
**Live demo:** Open GitHub, create 3 Issues in 3 repos, assign @copilot.
Come back 20 minutes later to show PRs appearing. Customer sees real parallel execution.

---

## What to Prepare for the Scale Demo

### Option A: Live Coding Agent Demo (Most Compelling)
1. **3 repos** on GitHub, each with a different demo app + shared skills
2. Create Issues live, assign to @copilot
3. While waiting for PRs, walk through the pilot migration details
4. Switch back to GitHub to show PRs arriving
5. Open one PR, walk through the diff — identical patterns to the pilot

### Option B: Pre-recorded Coding Agent + Live VS Code
1. Pre-record the "assign 5 Issues → 5 PRs appear" flow (2 minutes)
2. Do the VS Code Agent Mode demo live for 1 app to show the interactive experience
3. Point: "What I just did manually, Coding Agent does autonomously for the rest"

### What You Need to Build
- **2-3 additional demo app repos** on GitHub with different codebases
- Each repo has `.github/copilot-instructions.md` and `.github/skills/`
- Each app should have different on-prem dependencies to show the skill handling variety
- Copilot Coding Agent enabled on the GitHub org

### The Key Customer Takeaway

> "We proved the pattern with 1 app in VS Code.
> Now we assign a GitHub Issue for each of your remaining 32 apps.
> Copilot Coding Agent opens 32 PRs. Your developers review and merge.
> **Your team's job shifts from writing migration code to reviewing migration PRs.**
> That's how 6 months becomes 2 weeks."

### Slide 4: "Each pattern = one skill"
Show the skills library structure. Most apps share patterns.

### Slide 5: "The math"
Show the acceleration: 12-21 days → 1-2 days per app.
30 apps: ~18 months traditional → ~2 months agentic.

### Slide 6: "Wave plan"
Show the phased execution with parallel developers.

### Slide 7: "Let's run it on your next app — live"
Pick app #2 from the portfolio. Run the same skill. Show it works on a different codebase.
