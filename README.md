# KeCaS — Keke, Cab and Shuttle Transport Management System

Backend for the KeCaS transport management system (a FUNAAB group project).
It manages the vehicle fleet (kekes, cabs and campus shuttles), routes and
fares, driver shifts, trip scheduling, passenger bookings, payment records,
public notices, feedback and role-based administration — with public plate
verification for passengers.

## Architecture

Clean Architecture with CQRS over MediatR (.NET 10):

```
Backend/
├── src/
│   ├── kecas.domain/            Entities, value objects, domain rules, errors
│   ├── kecas.application/       Commands/queries (MediatR), validators, abstractions
│   ├── kecas.infrastructure/    EF Core (PostgreSQL), repositories, Argon2id, security
│   └── kecas.api/               Minimal API endpoints, middleware, auth cookies
└── KeCaS.slnx
```

**Stack:** .NET 10 · EF Core 10 + Npgsql (Supabase PostgreSQL, snake_case) ·
MediatR · FluentValidation · Argon2id password hashing · Serilog ·
ASP.NET output caching on public GETs.

## Getting started

### 1. Prerequisites

- .NET 10 SDK
- A PostgreSQL database (Supabase works out of the box)

### 2. Configure environment

Copy `Backend/.env.example` to `Backend/.env` and fill in:

| Variable | Purpose |
|---|---|
| `ConnectionStrings__Database` | PostgreSQL connection string (Supabase session pooler) |
| `SEED_USERS_PASSWORD` | Password for the seeded test accounts |
| `Security__Licence_Key` | 32-byte base64 AES-256 key for licence encryption |
| `FeatureFlags__*` | Enable/disable optional modules (notices, booking, payments, feedback) |

Never commit `.env` — it is git-ignored. Real environment variables override
`.env` values.

### 3. Seed the database

```sh
cd Backend
dotnet run --project src\kecas.api -- seed
```

Applies EF migrations, then inserts idempotent seed data: zones, stops,
routes, sample fleet (KK-001 keke, CB-1 cab…), drivers, fares, and the test
accounts from the permission matrix (`admin@kecas.test`, `manager@kecas.test`,
`operator@kecas.test`, `driver@kecas.test`, `passenger@kecas.test`).

### 4. Encrypt existing licence numbers (NFR-05)

If the database was seeded before licence encryption was introduced, rewrite
the plaintext licence numbers once:

```sh
dotnet run --project src\kecas.api -- encrypt-licences
```

Idempotent — rows already holding `enc1:` ciphertext are skipped. Requires
`Security__Licence_Key` to be set.

### 5. Run

```sh
dotnet run --project src\kecas.api
```

The API listens on `http://localhost:5118`; Swagger UI is available in
Development at `/swagger`. Health check: `GET /healthz`.

## API overview

All endpoints live under `/api/v1`, JSON in and out. Errors follow the
design-doc envelope (§6.1): a `problem+json` body with a `code` extension —
400 validation, 401 not signed in, 403 not permitted, 404 not found,
409 conflict, 422 business rule, 429 rate limited.

| Group | Endpoints |
|---|---|
| Auth | login, logout, me, change-password (session cookie `kecas_session`) |
| Users | staff directory (admin) |
| Network | zones, stops, routes, route stops |
| Fares | propose → approve/reject → supersede lifecycle |
| Fleet | vehicles, drivers (licence numbers always masked in responses) |
| Shifts | driver check-in / check-out, one open shift per driver and vehicle |
| Trips | schedule, start, complete, cancel (trip overlap = 409) |
| Public | published routes, live notices, plate verification |
| Bookings | request → confirm (capacity-checked, BR-09) / reject; `mine` view |
| Payments | record-only ledger (money moves outside the system, DD-08) |
| Feedback | anonymous submit → staff review |
| Notices | audience-targeted notices with lifecycle (draft → published → expired) |
| Reports | trips by period, fleet status, driver activity, availability history, fare history (JSON or `format=csv`) |
| Audit | append-only audit trail viewer (admin) |

Optional modules are behind feature flags: a disabled module responds
`404` with `code=Module.Disabled`.

## Security

- **Sessions** — httpOnly `SameSite=Lax` cookie holding an Argon2id-verified,
  server-side session (24 h). Permission checks happen server-side (§8.1
  matrix); permissions come from roles, never the client.
- **CSRF** (NFR-03) — double-submit token: every response carries a readable
  `csrf` cookie and every state-changing request (`POST/PUT/PATCH/DELETE`
  under `/api`) must echo it in the `X-CSRF-Token` header. Login is exempt
  and rotates the token. Missing/forged → `403 Csrf.Invalid`. Browser
  clients: read the `csrf` cookie, send the header.
- **Rate limiting** (DD-12) — public plate verification is limited to
  20 requests/hour per IP; excess → `429 RateLimiter.Limited`.
- **Licence encryption at rest** (NFR-05) — driver licence numbers are
  AES-256-GCM encrypted in the database (`enc1:` prefix). The key lives in
  `Security__Licence_Key`. Legacy plaintext rows still read (masked) until
  `encrypt-licences` rewrites them.
- **Masking** (FR-06) — licence numbers never leave the API unmasked.
- **Audit** — staff actions append audit events in the same transaction
  (DD-11).

## Migrations

```sh
dotnet ef migrations add <Name> --project src\kecas.infrastructure --startup-project src\kecas.api
dotnet run --project src\kecas.api -- seed   # applies migrations + seeds
```
