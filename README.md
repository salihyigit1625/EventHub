# EventHub

Event management API for publishing events, selling tickets, waitlists, wallet payments, and gate check-in.

Built with ASP.NET Core (Clean Architecture), JWT auth, SQL Server, and in-process background workers.

---

## Features

- **Roles:** Attendee, Organizer, GateStaff, Admin
- **Events & media:** draft → publish → cancel; secure poster upload/download
- **Ticketing:** purchase, cancel, inventory, per-user limits, waitlist holds
- **Wallet & payments:** demo deposits, receipts (`/payments/mine`)
- **Gate check-in:** event-scoped scans with time window + concurrency guards
- **Workers:** auto-complete ended events; expire waitlist holds and release stock
- **Security:** ownership checks, rate limits, hardened file pipeline

Full white-box assessment and remediation history:  
**[Security pentest report](docs/security/pentest-report.md)** · [PDF](docs/security/EventHub-Security-Audit-Report.pdf)

---

## Stack

| Layer | Tech |
|-------|------|
| API | ASP.NET Core / .NET 10 |
| Data | EF Core + SQL Server |
| Auth | JWT + refresh tokens |
| Docs | Swagger / OpenAPI |
| Tests | NUnit |

**Projects:** `Api` · `Application` · `Domain` · `Repository` · `Infrastructure` · `Tests`

---

## Quick start

### Docker (recommended)

```bash
docker compose up --build
```

- API: http://localhost:8080  
- Swagger: http://localhost:8080/swagger  

### Local

1. Start SQL Server (compose `sqlserver` service is enough).
2. Confirm `ConnectionStrings:DefaultConnection` in `src/EventHub.Api/appsettings.json`.
3. Run:

```bash
dotnet run --project src/EventHub.Api
dotnet test src/EventHub.Tests
```

Migrations apply automatically on startup; seed data is loaded once.

---

## Seed accounts

| Role | Email | Password |
|------|-------|----------|
| Admin | `admin@eventhub.local` | `Admin123!` |
| Organizer | `organizer@eventhub.local` | `Organizer123!` |
| Attendee | `attendee@eventhub.local` | `Attendee123!` |
| GateStaff | `gatestaff@eventhub.local` | `GateStaff123!` |

---

## Background workers

Configured under `Worker` in `appsettings.json`:

| Setting | Default | Purpose |
|---------|---------|---------|
| `Enabled` | `true` | Toggle jobs |
| `IntervalSeconds` | `60` | Poll interval |
| `BatchSize` | `100` | Max rows per tick |

**Jobs**
- Event completion (`Published` → `Completed` after `EndDate`)
- Waitlist hold expiry (`Notified` → `Expired`, restore inventory)

---

## API overview

| Area | Prefix |
|------|--------|
| Auth | `/api/auth` |
| Events | `/api/events` |
| Tickets | `/api/tickets` |
| Waitlist | `/api/waitlist` |
| Wallet / payments | `/api/wallet`, `/api/payments` |
| Gate staff | `/api/gate-staff` |
| Documents | `/api/documents` |
| Admin | `/api/admin` |

Explore interactive docs via Swagger after startup.

---

## Security

Remediation landed in PRs `#1`–`#11` (auth, events/media, documents, tickets, wallet, waitlist, gate/organizers, admin, business-logic critical/high/medium).

See: [docs/security/pentest-report.md](docs/security/pentest-report.md)