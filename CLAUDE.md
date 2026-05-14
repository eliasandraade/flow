# Flow — Engineering Operating Rules

This file defines how engineering work is conducted on this project. It is the authoritative source for working conventions, architectural constraints, and the definition of done. All contributors must read and follow this document before making any changes.

---

## Project Overview

**Flow** is a corporate innovation lifecycle management platform. Its purpose is to connect operational problems to ideas, formal projects, tracked execution, and measurable business outcomes. Flow is not an idea repository — it is a governance and traceability system for organizational innovation.

The system enforces a structured pipeline:

```
IDEA → ANALYSIS → APPROVAL → PROJECT → EXECUTION → RESULT
```

Every transition is an event. Every event is recorded. Nothing is overwritten.

---

## Core Principles

### 1. End-to-End Completeness Over Partial Features

Every implemented feature must work completely across all involved roles. A partially working screen is worse than a missing screen — it creates false confidence and breaks trust. Do not ship half-implemented flows. If a feature cannot be completed in the current phase, remove it from scope explicitly.

### 2. Traceability as a First-Class Concern

The audit log and project snapshots are not optional add-ons. They are core infrastructure. Every state transition must produce an `AuditLog` entry and, for projects, a `ProjectSnapshot` within the same database transaction. These are the foundation of the governance model. Skipping them for convenience is not acceptable.

### 3. Clean Architecture Discipline

The project uses Clean Architecture with strict layer separation:

```
Domain       — Business entities, state machines, domain logic. No framework dependencies.
Application  — Commands, queries, handlers, interfaces. Orchestrates domain logic.
Infrastructure — EF Core, database, external adapters. Implements application interfaces.
API          — Controllers, DTOs, middleware, DI wiring. Thin layer over application.
```

- Domain must not reference Application, Infrastructure, or API.
- Application must not reference Infrastructure or API.
- Dependencies always point inward.
- DTOs do not leak into the Domain layer.
- Domain entities are never returned directly from API endpoints.

### 4. Lean Implementation — No Unnecessary Complexity

The MVP must feel complete, not overbuilt. Do not introduce abstractions, patterns, or infrastructure that the current feature set does not require. Three similar lines of code are preferable to a premature abstraction. The system is designed to grow — do not pre-grow it.

---

## Working Rules

### Think Before Coding

Before implementing any feature:

1. Identify the affected domain entities and state transitions.
2. Confirm the audit log and snapshot behavior.
3. Identify which roles have access and what the authorization rule is.
4. Check for existing patterns in the codebase before introducing new ones.

### When in Doubt, Ask

Use `AskUserQuestion` when a requirement is ambiguous, when a decision has significant architectural impact, or when two valid approaches exist with meaningful trade-offs. Do not make silent assumptions on consequential decisions.

### Avoid Overengineering

Do not:
- Add configuration for things that do not need to vary.
- Create base classes or generic helpers before three concrete cases exist.
- Add error handling for scenarios that cannot occur given the system's invariants.
- Design for hypothetical future requirements.

Do:
- Follow existing patterns in the codebase.
- Write the simplest code that correctly implements the requirement.
- Keep files focused and small.

### Prioritize Clarity and Maintainability

Code is written once and read many times. Names must be explicit. Logic must be localized. A future engineer must be able to understand any unit of the system by reading it — without needing to reconstruct context from surrounding files.

---

## Architectural Decisions

These decisions are fixed for the MVP. Do not revisit without explicit approval.

| Concern | Decision |
|---|---|
| Architecture pattern | Modular monolith with Clean Architecture |
| Backend | ASP.NET Core 8 Web API, C# |
| ORM | Entity Framework Core 8 |
| Database | Azure SQL Database |
| Mobile | React Native (Expo managed workflow) |
| Web (Leadership) | React + Vite |
| Authentication | ASP.NET Core Identity + JWT (email/password primary) |
| Traceability | Hybrid: append-only AuditLog + synchronous ProjectSnapshot per transition |
| Cloud | Azure (Container Apps, Static Web Apps, Azure SQL) |
| ROI | Manual entry; estimated and actual tracked independently |
| Scale target | 1,000+ users, single organization |

---

## Infrastructure Constraints

The following are explicitly excluded from the MVP. Do not introduce them without approval.

| Technology | Status | Reason |
|---|---|---|
| Redis | Excluded | No cache layer needed at current scale |
| SignalR | Excluded | Dashboard uses polling; real-time not required for MVP |
| Background jobs (Hangfire, Quartz) | Excluded | Snapshots are synchronous; no periodic tasks |
| Microservices | Excluded | Modular monolith is sufficient and simpler |
| Azure AD SSO (wired) | Excluded | Abstraction prepared; not wired for MVP |
| ERP integration | Excluded | Abstraction prepared; manual entry for MVP |

### Prepared Abstractions

The following interfaces exist but use no-op or manual implementations. They are seams for future upgrades — not active infrastructure:

- `IAuthProvider` — for future Azure AD SSO swap-in
- `ICacheService` — for future Redis swap-in
- `IFinancialDataSource` — for future ERP integration
- `INotificationService` — for future push/email notifications

---

## State Machine Rules

State transitions are domain operations, not field updates. The following rules apply to all state machines in this system:

1. Transitions are defined explicitly in the domain entity. Only permitted transitions may execute.
2. Every transition produces an `AuditLog` entry with `ActorId`, `ActorName`, `OldValue`, `NewValue`, `Reason` (when required), and `Timestamp`.
3. Every project transition also produces a `ProjectSnapshot` capturing the **full project state** at the moment of transition.
4. Both the state change, the audit log write, and the snapshot write occur within a single database transaction.
5. `BlockedReason` is required on every transition into the `Blocked` state.
6. `Reason` in `AuditLog` is required for `Reject`, `Cancel`, and `Block` transitions, and optional elsewhere.

### Absolute Prohibition: Direct Database Writes

**No state-changing operation may bypass domain logic, audit logging, or snapshot creation.**

This means:

- Controllers must never call `DbContext` directly for mutation operations.
- Repositories must never be called directly from controllers or middleware.
- All writes to `Idea`, `Project`, and `Result` entities must pass through the Application layer command handlers.
- Command handlers are responsible for orchestrating the state transition, the `AuditLog` write, and the `ProjectSnapshot` write within a single unit of work.
- Raw SQL mutations (`ExecuteSqlRaw`, `ExecuteSqlInterpolated`) are forbidden on any audited entity under any circumstances.
- Seed data and migrations may write directly to the database only for reference data (roles, guidelines defaults) — never for domain entities that participate in the audit trail.

Any violation of this rule silently breaks the governance model. It is treated as a critical defect, not a minor shortcut.

---

## ROI Rules

- **Estimated ROI** represents the planning-phase projection. It is populated before or during execution.
- **Actual ROI** represents the post-completion measurement. It is populated after the project completes.
- Neither overwrites the other. Both are independently stored and traceable via the `AuditLog`.
- ROI formula: `(Revenue + Savings - Cost) / Cost × 100`. Division by zero returns `null`.
- ROI is computed in the application layer on every save — not as a database computed column.

---

## Definition of Done

A feature is complete when:

1. **It works end-to-end across all affected roles.** Every role that interacts with the feature can complete their full workflow without errors.
2. **It is properly authorized.** Role-based access control is enforced at the API layer. Unauthorized requests return `403`, not `404`.
3. **It is traceable.** Every state-changing operation produces an `AuditLog` entry. Every project transition produces a `ProjectSnapshot`.
4. **It is validated.** All input is validated at the API boundary. Invalid input returns `400` with a structured error response.
5. **It is documented.** The OpenAPI spec reflects the endpoint. If the feature introduces new domain behavior, the relevant section of the design spec is updated.
6. **It is consistent with the architecture.** No layer violations, no leaked DTOs into the domain, no direct infrastructure calls from controllers.

---

## Module Boundaries

| Module | Owns |
|---|---|
| Auth | User registration, login, JWT issuance, refresh token management |
| Ideas | Idea lifecycle, comments, prioritization, approval/rejection |
| Projects | Project lifecycle, state transitions, owner assignment, field management |
| Tracking | AuditLog writes, ProjectSnapshot writes, timeline queries |
| Results | ROI entry, estimated/actual tracking, payback calculation |
| Dashboard | Aggregation queries: conversion rate, status distribution, ROI totals, avg completion time |
| Gamification | Points ledger, point award logic, operator score |
| Guidelines | Strategic guideline CRUD |

Cross-module calls go through application service interfaces — never directly between infrastructure implementations.

---

## File and Naming Conventions

- Commands: `{Action}{Entity}Command.cs` — e.g., `ApproveIdeaCommand.cs`
- Queries: `Get{Entity}{Qualifier}Query.cs` — e.g., `GetProjectTimelineQuery.cs`
- Handlers: co-located with their command/query in the Application layer
- Entities: singular noun — e.g., `Idea.cs`, `Project.cs`
- Controllers: plural noun — e.g., `IdeasController.cs`, `ProjectsController.cs`
- DTOs: `{Entity}{Purpose}Dto.cs` — e.g., `IdeaSummaryDto.cs`, `ProjectDetailDto.cs`

---

## Design Specification

The authoritative design document is located at:

```
docs/superpowers/specs/2026-05-13-flow-mvp-design.md
```

This file defines the full domain model, state machines, API surface, and execution phases. Consult it before making any structural change.
