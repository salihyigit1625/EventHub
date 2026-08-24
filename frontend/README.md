# EventHub Web (Nuxt 4)

Browse published events, sign in to buy tickets, and manage wallet / waitlist. Organizer studio, admin console, and gate check-in open by role.

## Run

### Docker (recommended)

From the repo root:

```bash
docker compose up --build
```

| Service | URL |
|---------|-----|
| Web | http://localhost:3000 |
| API | http://localhost:8080 |
| Swagger | http://localhost:8080/swagger |

### Local

1. Start the API (`dotnet run --project src/EventHub.Api` → usually `http://localhost:5170`).
2. Frontend:

```bash
cd frontend
cp .env.example .env
# keep NUXT_PUBLIC_API_BASE aligned with your API URL
npm install
npm run generate:api   # regenerates client from Swagger when API is up
npm run dev
```

http://localhost:3000

## Role flows

| Role | What they do |
|------|----------------|
| Guest | Browse events on the home page; sign in / register to buy |
| Attendee | Buy / cancel tickets, top up wallet, join waitlists |
| Organizer | Create events + poster + ticket types, publish; update company profile |
| GateStaff | Validate tickets on the check-in screen (camera or manual code) |
| Admin | Approve organizers, view stats, assign gate staff |

All roles: click the header name chip for **Profile** (`/profile`).

## Seed accounts

| Role | Email | Password |
|------|-------|----------|
| Admin | `admin@eventhub.local` | `Admin123!` |
| Organizer | `organizer@eventhub.local` | `Organizer123!` |
| Attendee | `attendee@eventhub.local` | `Attendee123!` |
| GateStaff | `gatestaff@eventhub.local` | `GateStaff123!` |

Root overview: [../README.md](../README.md)
