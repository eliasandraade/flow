# Flow — Project Memory

This document is the persistent memory of all significant product decisions, architectural choices, scope boundaries, and technical constraints for the Flow project. It is updated as decisions are made or revised. It is not a changelog — it is the current authoritative state of project knowledge.

---

## Product Definition

### What Flow Is

Flow is a **corporate innovation lifecycle management platform**. It provides a structured, auditable system for organizations to capture operational problems as ideas, evaluate and approve them through a governance process, convert approved ideas into tracked projects, execute those projects with full visibility, and measure their impact through quantified business results.

### Core Problem It Solves

Most organizations collect ideas but lack the infrastructure to evaluate, prioritize, and execute them systematically. Promising initiatives stall, never get measured, and leave no trace. Flow solves this by enforcing a formal pipeline with role-based governance, immutable event history, and ROI tracking — making innovation accountable rather than aspirational.

### Target Users and Roles

| Role | Who They Are | What They Do in Flow |
|---|---|---|
| **Operator** | Frontline employees, contributors | Submit ideas based on real operational problems they observe |
| **Manager** | Team leads, project owners | Review ideas, approve or reject them, convert ideas to projects, drive execution |
| **Leadership** | Directors, executives | Define strategic direction, monitor dashboard KPIs, evaluate ROI outcomes |

---

## System Flow

```
IDEA → ANALYSIS → APPROVAL → PROJECT → EXECUTION → RESULT
```

Each stage is a formal state. Transitions are events — they are recorded immutably in the audit log. The system never overwrites history.

---

## State Machines

### Idea Lifecycle

```
[Draft]
  │
  ▼ submit (Operator)
[UnderReview]
  │
  ├──▶ [Approved]   Manager approves — points awarded to submitter
  │
  └──▶ [Rejected]   Manager rejects — comment required
```

### Project Lifecycle

```
[Planned]
  │
  ├──▶ [InProgress]  Manager starts project
  │       │
  │       ├──▶ [Completed]   Manager marks complete — Result record prompted
  │       ├──▶ [Cancelled]   Manager cancels — reason required
  │       └──▶ [Blocked]     Manager blocks mid-execution — reason required
  │
  └──▶ [Blocked]     Manager blocks before execution — reason required
            │         (represents pre-execution dependencies, approvals, resource gaps)
            ├──▶ [InProgress]  Manager unblocks — resumes or starts execution
            └──▶ [Cancelled]   Manager cancels while blocked — reason required
```

**Rules:**
- `BlockedReason` is required on every `→ Blocked` transition.
- `AuditLog.Reason` is required for `Reject`, `Cancel`, and `Block` transitions.
- All transitions are guarded. Invalid transitions return a domain error and do not persist.
- `CompletedAt` is set automatically on `→ Completed`.

---

## Confirmed Decisions

| Concern | Decision | Rationale |
|---|---|---|
| **Tenancy** | Single organization | MVP serves one company; no tenant isolation needed |
| **Architecture** | Modular monolith, Clean Architecture | Fastest to build, sufficient for 1,000+ users, decomposable later |
| **Backend** | ASP.NET Core 8, C# | Team preference, strong domain modeling, excellent for governance systems |
| **ORM** | Entity Framework Core 8 | Mature, well-integrated with ASP.NET Core Identity |
| **Database** | Azure SQL Database | Relational integrity, strong EF Core support, Azure-native |
| **Mobile** | React Native (Expo managed workflow) | Cross-platform iOS/Android, shares TypeScript with web client |
| **Web** | React + Vite | Leadership dashboard only; not a full web app |
| **Authentication** | ASP.NET Core Identity + JWT | Email/password primary; IAuthProvider abstraction ready for SSO |
| **Traceability** | Hybrid: append-only AuditLog + synchronous ProjectSnapshot | Governance-grade auditability without event sourcing complexity |
| **Snapshots** | Synchronous on every state transition, same DB transaction | No background job dependency; guaranteed consistency |
| **ROI** | Manual entry; estimated and actual tracked independently | Avoids ERP integration complexity; IFinancialDataSource abstraction prepared |
| **ROI phases** | Estimated (planning) and actual (post-completion) are separate, independently stored | Full traceability of when and by whom each phase was recorded |
| **Dashboard** | Includes conversion rate, avg completion time, ROI totals, status distribution, active blockers | Supports leadership visibility and bottleneck identification |
| **Gamification** | Points ledger with reason and entity reference | Lightweight recognition; full traceability of every award |
| **Blocked state** | Reachable from both Planned and InProgress | Represents pre-execution dependencies, not only runtime interruption |
| **Cloud** | Azure (Container Apps, Static Web Apps, Azure SQL) | Aligns with .NET ecosystem and enterprise Microsoft stack |
| **Scale** | 1,000+ users, single organization | Informs indexing strategy and query design; no horizontal scaling for MVP |
| **AuditLog reason** | Optional field on all entries; required on Reject, Cancel, Block | Adds context to governance-critical transitions |
| **No direct DB writes** | All mutations to audited entities must pass through domain logic and command handlers | Bypassing domain logic silently breaks the audit trail — treated as critical defect |
| **ProjectSnapshot schema version** | `SchemaVersion` field on every snapshot | Enables future migration and interpretation of snapshots captured under old schemas |
| **Blocked as first-class KPI** | Dashboard surfaces blocked count, blocked project list, and bottleneck index explicitly | Bottleneck visibility is a core product differentiator, not a filter option |

---

## Scope — What Is Included in MVP

| Module | Included Capabilities |
|---|---|
| **Authentication** | Email/password registration and login, JWT access + refresh tokens, role-based authorization |
| **Strategic Guidelines** | Leadership CRUD; read-only for all other roles |
| **Ideas** | Submit, list, filter by status, comment, prioritize, approve, reject |
| **Projects** | Create from idea or standalone, assign owner/deadline/cost, full state machine |
| **Project Tracking** | Immutable AuditLog per entity, ProjectSnapshot per transition, timeline endpoint |
| **Results & ROI** | Manual entry of estimated and actual revenue/savings/cost; computed ROI; payback period |
| **Dashboard** | Conversion rate, avg completion time, project status distribution, global ROI, blocked project count (first-class KPI), blocked project list with reason and duration, bottleneck index |
| **Gamification** | Points ledger with reason and entity reference; operator score; point award on idea approval |
| **Mobile App** | All three role flows fully implemented; connected to live API |

---

## Non-Goals — What Is Intentionally Excluded from MVP

These items are known, considered, and consciously deferred. They are not forgotten.

| Item | Status | Notes |
|---|---|---|
| Azure AD SSO | Prepared only | `IAuthProvider` abstraction in place; wiring deferred |
| ERP/finance integration | Prepared only | `IFinancialDataSource` interface defined; manual entry is the implementation |
| Redis caching | Prepared only | `ICacheService` abstraction in place; no-op implementation used |
| SignalR real-time updates | Prepared only | Hub scaffolded; dashboard uses polling |
| Background jobs / schedulers | Excluded | No periodic tasks; snapshot logic is a callable service |
| Multi-tenancy | Excluded | Single organization only |
| Microservices | Excluded | Modular monolith is the architectural target |
| BFF layer | Deferred to Phase 2+ | Single API serves both mobile and web |
| File attachments | Excluded | No blob storage for MVP |
| Email notifications | Excluded | `INotificationService` abstraction in place; no-op implementation |
| Admin user management UI | Excluded | Role assignment via seeding or admin endpoint only |
| Public API / webhooks | Excluded | Internal system only |
| Bulk import / export | Excluded | Not required for MVP workflows |
| Advanced BI / analytics | Excluded | Dashboard covers core KPIs only |

---

## Future Extensions

These are validated directions for post-MVP development. They are not speculative — they are the natural next layer of the system.

| Extension | Readiness | What Unlocks It |
|---|---|---|
| Azure AD SSO | `IAuthProvider` abstraction in place | Wire MSAL and Azure AD tenant configuration |
| ERP/finance integration | `IFinancialDataSource` interface defined | Implement ERP-specific adapter |
| Redis caching | `ICacheService` abstraction in place | Register Redis implementation in DI |
| SignalR real-time | Hub scaffolded | Connect dashboard to hub; publish events on state changes |
| BFF layer | Clean API surface established | Extract mobile and web BFFs from the core API |
| Multi-tenancy | Domain entities are tenant-unaware today | Add `TenantId` to all entities; add tenant resolution middleware |
| Microservices | Module boundaries are clean | Extract individual modules with their own database |
| Background jobs | Snapshot service is a callable class | Add Hangfire or Quartz; schedule snapshot and digest jobs |

---

## Success Criteria

These define what a successful MVP deployment looks like. They are observable, measurable, and tied directly to the system's core purpose.

### Innovation Pipeline Health

| Criterion | Target | How Measured |
|---|---|---|
| Ideas are submitted and reach a decision (approved or rejected) | 100% of submitted ideas have a final status within the system | Idea status distribution in dashboard |
| Approved ideas are converted to projects | Conversion rate is visible and non-zero | Dashboard conversion rate metric |
| No idea is permanently stuck in `UnderReview` without action | Managers have a clear, filterable review queue | Idea queue with status filter |

### ROI Traceability

| Criterion | Target | How Measured |
|---|---|---|
| Estimated ROI is captured before or during project execution | Result record exists with estimated values for every active project | Result completeness check |
| Actual ROI is captured after project completion | Result record has actual values for every completed project | Dashboard ROI totals |
| Estimated vs. actual ROI discrepancy is visible per project | Both values shown side-by-side in project detail | Project result detail view |
| Every ROI entry is attributable | `RecordedBy` and `RecordedAt` / `UpdatedAt` fields populated | Audit log for result entity |

### Bottleneck Visibility

| Criterion | Target | How Measured |
|---|---|---|
| Blocked projects are surfaced immediately | Dashboard shows blocked count as a first-class KPI | Blocked projects count metric |
| Each blocked project exposes its reason and duration | Leadership can identify who is blocking and for how long | Blocked project list with `BlockedReason` and days in state |
| Bottleneck index is tracked | Percentage of active projects currently blocked is visible | Bottleneck index metric |
| Unblocking is auditable | Every `→ Blocked` and `→ InProgress` (unblock) transition is in the audit log | Project timeline view |

### Audit Completeness

| Criterion | Target | How Measured |
|---|---|---|
| Every state transition is recorded | Zero state changes without a corresponding `AuditLog` entry | Spot-check via project timeline endpoint |
| Every project transition has a snapshot | `ProjectSnapshot` count equals state transition count per project | Snapshot endpoint |
| Governance-critical transitions carry a reason | `Reject`, `Cancel`, `Block` transitions always have a `Reason` field | AuditLog query |

### User Experience

| Criterion | Target | How Measured |
|---|---|---|
| Operators can submit an idea in under 2 minutes | Idea submission flow is 3 screens or fewer | Manual flow test |
| Managers can action a review queue item without leaving the list | Approve/reject available inline or from detail with one navigation step | Manual flow test |
| Leadership dashboard loads in under 3 seconds | Aggregation queries are indexed and optimized | Manual timing on 1,000+ record dataset |

---

## Technical Constraints

| Constraint | Rule |
|---|---|
| No Redis | Do not introduce a cache layer for MVP |
| No SignalR (active) | Dashboard uses polling; hub exists but is not connected |
| No background jobs | All operations are synchronous; no scheduled tasks |
| No microservices | Single deployable backend |
| Synchronous snapshots | Snapshots within the same DB transaction as their triggering transition |
| Layer discipline | Domain has no external dependencies; Application has no infrastructure references |
| Audit completeness | Every state-changing operation produces an AuditLog entry |
| ROI independence | Estimated and actual ROI are stored and updated independently |
| Traceability of points | Every point award creates a PointLedgerEntry with reason and entity reference |

---

## Execution Phases

| Phase | Scope |
|---|---|
| **Phase 1** | Solution structure, DB schema + migrations, authentication, base repositories, audit log infrastructure |
| **Phase 2** | Ideas module, Projects module, state machine, AuditLog writes, ProjectSnapshots |
| **Phase 3** | Results & ROI engine, dashboard aggregations, gamification points |
| **Phase 4** | React Native mobile app — all three role flows, connected to live API |
| **Phase 5** | End-to-end verification, OpenAPI spec, technical documentation |

---

## Design Specification

The full domain model, state machines, API surface, entity definitions, and architectural decisions are documented in:

```
docs/superpowers/specs/2026-05-13-flow-mvp-design.md
```

This file is the authoritative technical specification. It must be consulted before making any structural change to the domain model, API surface, or module boundaries.
