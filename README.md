# EventHub

End-to-end platform for event discovery, ticketing, wallet payments, waitlists, and gate check-in.

**API:** ASP.NET Core (Clean Architecture), JWT, SQL Server, in-process background workers  
**Web:** Nuxt 4 + Vue 3 + Pinia (`frontend/`)

---

## Features

### Backend
- **Roles:** Attendee, Organizer, GateStaff, Admin
- **Events & media:** draft → publish → cancel; secure poster upload/download
- **Ticketing:** purchase, cancel, inventory, per-user limits, waitlist holds
- **Wallet & payments:** demo deposits, receipts (`/api/payments/mine`)
- **Gate check-in:** event-scoped scans with time window + concurrency guards
- **Workers:** auto-complete ended events; expire waitlist holds and release stock
- **Security:** ownership checks, rate limits, hardened file pipeline

### Web (`frontend/`)
- Discovery homepage: search, date/venue filters, event cards (price & occupancy)
- Role panels: attendee account, organizer studio, admin console, check-in kiosk
- Shared **Profile** page (`/profile`) for all roles
- Camera or manual code gate check-in; ticket QR codes
- Details: [frontend/README.md](frontend/README.md)

Full white-box assessment and remediation history:  
**[Security pentest report](docs/security/pentest-report.md)** · [PDF](docs/security/EventHub-Security-Audit-Report.pdf)

---

## Stack

| Layer | Tech |
|-------|------|
| Web | Nuxt 4, Vue 3, Pinia, Zod |
| API | ASP.NET Core / .NET 10 |
| Data | EF Core + SQL Server |
| Auth | JWT + refresh tokens |
| Docs | Swagger / OpenAPI |
| Tests | NUnit |

**Projects:** `EventHub.Api` · `Application` · `Domain` · `Repository` · `Infrastructure` · `Tests` · Web (`frontend/`)

---

## Quick start

### Docker (recommended)

```bash
docker compose up --build
```

| Service | URL |
|---------|-----|
| Web | http://localhost:3000 |
| API | http://localhost:8080 |
| Swagger | http://localhost:8080/swagger |
| SQL Server | `localhost:1433` (SA: `EventHub123!`) |

Compose starts three services: `sqlserver`, `api`, and `web`.

### Local (API + Web separately)

1. Start SQL Server (compose `sqlserver` alone is enough).
2. Confirm `ConnectionStrings:DefaultConnection` in `src/EventHub.Api/appsettings.json`.
3. Run the API:

```bash
dotnet run --project src/EventHub.Api
# default: http://localhost:5170
```

4. Run the web app:

```bash
cd frontend
cp .env.example .env   # set NUXT_PUBLIC_API_BASE=http://localhost:5170
npm install
npm run generate:api   # regenerates client from Swagger when API is up
npm run dev            # http://localhost:3000
```

5. Tests:

```bash
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

After login, the UI opens the matching panel / check-in / profile. The header name chip goes to `/profile`.

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
| Ticket types | `/api/events/{id}/ticket-types`, `/api/ticket-types` |
| Tickets | `/api/tickets` |
| Waitlist | `/api/waitlist` |
| Wallet / payments | `/api/wallet`, `/api/payments` |
| Organizers | `/api/organizers` (`me` profile) |
| Gate staff | `/api/gate-staff` |
| Documents | `/api/documents` |
| Admin | `/api/admin` |

Explore interactive docs via Swagger after startup.

---

## Security

Remediation landed in PRs `#1`–`#11` (auth, events/media, documents, tickets, wallet, waitlist, gate/organizers, admin, business-logic critical/high/medium).

See: [docs/security/pentest-report.md](docs/security/pentest-report.md)
