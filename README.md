# Admitto

An open-source event ticketing and management API built with ASP.NET Core 9. Admitto lets organizers publish events and sell tickets, while attendees browse, book, and pay — all through a clean REST API.

---

## What it does

- **Events** — create, edit, and publish events with unique slugs and rich metadata
- **Ticketing** — define ticket types with capacities and sale windows; bookings are atomic and oversell-proof
- **Payments** — Paystack integration for payment initialization and verification
- **Notifications** — email notifications (SendGrid) for bookings, cancellations, reminders, and profile changes, delivered reliably through an outbox queue
- **Event discovery** — proxy to Ticketmaster's public API for browsing external events
- **Media** — upload images and videos per event, stored on AWS S3 or local disk
- **Reminders** — background service that sends event reminders on a configurable schedule, safe across multiple running instances
- **Auth** — JWT access tokens with refresh token rotation and secure password reset

---

## Tech stack

| Concern | Choice |
|---|---|
| Runtime | .NET 9 / ASP.NET Core |
| Database | SQL Server (EF Core 9) |
| Cache & locking | Redis (StackExchange.Redis) |
| Payments | Paystack |
| Email | SendGrid |
| File storage | AWS S3 / local disk |
| Event discovery | Ticketmaster Discovery API |
| Logging | Serilog → Console + Seq + rolling file |
| Auth | JWT (HMAC-SHA256) + refresh token rotation |

---

## Getting started

### Prerequisites

- .NET 9 SDK
- SQL Server (LocalDB works for development)
- Redis (optional locally — app starts without it)
- A Paystack account, SendGrid account, and AWS S3 bucket for full functionality

### 1. Clone and restore

```bash
git clone https://github.com/CodeFalcon363/Admitto.Api.git
cd Admitto.Api
dotnet restore
```

### 2. Configure secrets

Copy your settings into `Admitto.Api/appsettings.Development.json` (this file is gitignored). A minimal dev setup:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=AdmittoDB;Trusted_Connection=True;TrustServerCertificate=True;",
    "Redis": "localhost:6379"
  },
  "JwtSettings": {
    "SecretKey": "a-secret-key-at-least-32-characters-long"
  },
  "StorageSettings": { "Provider": "Local" },
  "RedisSettings": { "AbortOnConnectFail": false }
}
```

For production, supply secrets via environment variables (e.g. `ConnectionStrings__DefaultConnection`). Never commit real credentials.

### 3. Run migrations

```bash
dotnet ef migrations add InitialCreate --project Admitto.Core --startup-project Admitto.Api
dotnet ef database update --project Admitto.Core --startup-project Admitto.Api
```

### 4. Run the API

```bash
cd Admitto.Api
dotnet run
```

API docs are available at `/scalar/v1` once the app is running.

---

## Project structure

```
Admitto.Api/           # HTTP layer — controllers, middleware, DI wiring
Admitto.Core/          # Domain — entities, DTOs, enums, settings
Admitto.Infrastructure/ # I/O — EF repositories, services, background workers
```

Core has no external dependencies. Infrastructure depends only on Core. The API layer wires everything together.

---

## API overview

All routes are versioned under `/api/v1/`.

| Area | Base path |
|---|---|
| Auth | `/api/v1/auth` |
| Users | `/api/v1/users` |
| Events | `/api/v1/events` |
| Ticket types | `/api/v1/events/{id}/ticket-types` |
| Bookings | `/api/v1/bookings` |
| Payments | `/api/v1/payments` |
| Event media | `/api/v1/events/{id}/media` |
| Notifications (admin) | `/api/v1/admin/notification-rules` |
| Notification preferences | `/api/v1/notification-preferences` |

Three roles exist: `Admin`, `Organizer`, and `Attendee`. Role is set at registration and can be changed by an Admin.

---

## Contributing

1. Fork the repo and create a feature branch
2. Follow the existing layer boundaries — controllers stay thin, all I/O lives in Infrastructure
3. Add a migration for any `OnModelCreating` change
4. Keep user-facing strings in `ApiMessages.cs`
5. Open a pull request with a clear description of what changed and why

If you find a security issue, please open a private report rather than a public issue.

---

## License

MIT — see [LICENSE](LICENSE).
