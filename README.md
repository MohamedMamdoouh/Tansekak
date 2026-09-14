# Tansekak

**Vibe coded project** — built iteratively with AI-assisted development.

Tansekak (تنسيقك) is an admission eligibility checker for Egyptian Thanaweya Amma graduates. Students pick a track and total score; the app lists faculties they are eligible for against the **current published** admission year.

Students can also look up an imported Thanaweya result by seating number and see their track rank among peers.

Results are **indicative only**. Egypt’s official coordination portal decides final placement.

**Current-stage docs**

| Document | Role |
| --- | --- |
| [docs/PRD.md](docs/PRD.md) | As-built product spec |
| [docs/API.md](docs/API.md) | HTTP contract, error codes |
| [docs/DEPLOY.md](docs/DEPLOY.md) | Render + Neon + R2 checklist |

---

## Features

### Public (RTL Arabic)

| Feature | Route | Description |
| --- | --- | --- |
| Landing | `/` | Overview and entry points |
| College prediction | `/predict` | Track + score form |
| Prediction results | `/results` | Eligible faculties, client-side search, load-more |
| Thanaweya lookup | `/thanaweya-result` | Result by seating number (current year) |
| Track rank | `/track-rank` | Rank and percentile among the same track |
| Coordination guide | `/guide` | Static FAQ |
| Developer profile | `/designer` | Credits |

**Prediction**

- Eligible only: `student score >= cutoff`.
- Current published year, matching track bucket, and faculty `AllowedTracks`.
- Sorted closest cutoff first (`abs(score − cutoff)`).
- Frontend `pageSize: 20` with unlimited load-more. API default is 10, max 100.
- Search filters **already loaded** university/faculty names (client-side).

**Tracks**

| API value | Arabic | Notes |
| --- | --- | --- |
| `Science` | الشعبة العلمية | علوم and رياضة share this bucket |
| `Literature` | الشعبة الأدبية | |

`Mathematics` still exists on the domain enum for legacy rows and is canonicalized to `Science` everywhere.

Public brand is **تنسيقك**. API `appName` is `tansekak`. Admin chrome is **لوحة الإدارة**.

### Admin

Not linked from the public site. Sign in at `/admin/login`.

| Page | Route | Description |
| --- | --- | --- |
| Dashboard | `/admin` | Current year plus catalog and student-result counts (`cutoffsCount` is returned by the API but not shown) |
| Admission years | `/admin/years` | Create, edit, delete the single admission year |
| Cutoffs | `/admin/cutoffs` | CRUD for the **current** year |
| Import cutoffs | `/admin/import` | Science and/or Literature Markdown; each file replaces that track for the current year |
| Import student results | `/admin/import-results` | Excel for the **current** year (replaces all results for that year) |

Import pages block navigation with a progress overlay until the upload finishes or you confirm leaving.

**Development admin** (rejected in Production): `admin@tansekak.local` / `Admin@12345`. Production uses `AdminSeed__Email` and `AdminSeed__Password`.

**API-only** (no UI): governorates, universities, faculties, university–faculties.

---

## Tech stack

| Layer | Technology |
| --- | --- |
| Backend | ASP.NET Core 10, EF Core, PostgreSQL (Npgsql), Identity cookies |
| Frontend | Angular 19 standalone, RTL |
| Validation | FluentValidation |
| Excel | ClosedXML |
| Large uploads | Cloudflare R2 (presigned PUT) |
| Tests | xUnit |
| CI | GitHub Actions (build + test) |
| Deploy | Docker monolith on Render, Neon Postgres |

Clean Architecture: `Domain` → `Application` (services + DTOs, no MediatR, no repositories) → `Infrastructure` → `Api`. Integer business IDs use `EntityIdAllocator`; Identity tables use PostgreSQL identity columns.

---

## Project structure

```
Tansekak/
├── client/                      # Angular 19 SPA
├── SeededData/                  # Catalog JSON (copied to API output as SeedData/)
│   └── cutoffs/                 # Sample Markdown — upload via admin, not auto-seeded
├── docs/
├── tests/
│   ├── Tansekak.Application.Tests/
│   ├── Tansekak.Infrastructure.Tests/
│   └── Tansekak.Api.Tests/      # On disk; not in Tansekak.sln / CI
├── src/Tansekak.{Api,Application,Domain,Infrastructure}/
├── Dockerfile
└── Tansekak.sln
```

---

## Prerequisites

**Local:** .NET 10 SDK, Node.js 20+, PostgreSQL 16+ (or Neon).

**Production:** Neon Postgres, Render web service, Cloudflare R2 for Excel files **>20 MB**.

---

## Quick start (local)

### 1. Database

Default connection in `src/Tansekak.Api/appsettings.Development.json`:

```
Host=localhost;Port=5432;Database=Tansekak;Username=postgres;Password=postgres
```

### 2. API

```powershell
cd src/Tansekak.Api
dotnet run
```

- `http://localhost:5080`
- Health: `/health` → `{ "status": "healthy" }`
- OpenAPI (Development only): `/openapi/v1.json` (no Swagger UI)
- Startup: migrations, catalog seed if empty, bootstrap year **2027** (max 320, current), dev admin user

### 3. Frontend

```powershell
cd client
npm install
npm start
```

- `http://localhost:4200` (proxies `/api` to `:5080`)

### 4. First-run cutoffs (required for predictions)

Cutoffs are **not** seeded. From a **local clone**, open `/admin/login`, confirm the year on `/admin/years`, then upload:

- `SeededData/cutoffs/science-2026.md` → Science
- `SeededData/cutoffs/literature-2026.md` → Literature

`2026` is the official source cycle. Files attach to the **current published year** (bootstrap **2027**). They are **not** inside the Docker image.

---

## Production

See [docs/DEPLOY.md](docs/DEPLOY.md).

| Layer | Provider |
| --- | --- |
| App + SPA | Render Docker web service (`0.0.0.0:$PORT`) |
| Database | Neon (`ConnectionStrings__DefaultConnection` or `DATABASE_URL`) |
| Storage | Cloudflare R2 for Excel **>20 MB** |

Do **not** set `Frontend__Origin` in production — SPA and API share one origin.

Prefer Render **Starter** if large imports must not be interrupted by free-tier spin-down.

---

## Configuration

| Key | Purpose |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | Npgsql keyword string, or use `DATABASE_URL` |
| `Tansekak:AppName` | Returned by `/api/config` |
| `AdminSeed:Email` / `Password` | First-run admin |
| `Frontend:Origin` | CORS for local Angular (`http://localhost:4200`) only |
| `R2:*` | Large Excel uploads |

Env vars use `__` (`R2__AccountId`). Production rejects localhost connection strings and the dev admin credentials. Missing R2 logs a warning; uploads over 20 MB then return HTTP 503.

---

## Database

Seeded once when `Governorates` is empty: `Governorates.json`, `Universities.json`, `Faculties.json`, `UniversityFaculties.json`. After that the database is the source of truth.

Entities: Governorate, University (`Public` / `Institute`), Faculty (`AllowedTracks`), UniversityFaculty, AdmissionYear, AdmissionCutoff, StudentResult, ImportJob, EntityIdSequence.

EF migrations apply automatically on startup.

---

## API and import

Envelope: `{ success, message, errorCode?, data, errors? }`. Failures use a stable `errorCode` and Arabic `message`. Success default message is English. `/health` is not wrapped.

Auth is **cookie Identity**, role `Administrator`.

Full contract: [docs/API.md](docs/API.md).

---

## Testing

```powershell
dotnet test Tansekak.sln --configuration Release
```

That runs **Application** and **Infrastructure** tests (track rules, prediction, admission year rules, seeded Markdown parse, import jobs, connection resolution). `tests/Tansekak.Api.Tests` exists (global exception handler) but is **not** in the solution, so CI does not run it. There are no integration or E2E projects.

---

## Building for production manually

```powershell
docker build -t tansekak .
docker run -p 8080:8080 `
  -e ConnectionStrings__DefaultConnection="Host=...;SSL Mode=Require" `
  -e AdminSeed__Email="admin@example.com" `
  -e AdminSeed__Password="your-strong-password" `
  tansekak
```

Or without Docker: `npm ci` + production Angular build, `dotnet publish` the API, copy `client/dist/client/browser/*` into `publish/wwwroot`.

---

## Disclaimer

Tansekak uses official cutoff lists to estimate eligibility. It does not replace Egypt’s electronic coordination portal. Actual admission depends on demand, seats, preference order, and official rules for the current cycle.
