# Tansekak

**Vibe coded project** — built iteratively with AI-assisted development.

Tansekak is an admission eligibility checker for Egyptian Thanaweya Amma (high school) graduates. Students enter their academic track and total score; the app compares those values against official cutoff scores for the current admission year and shows which university faculties they are likely eligible for.

Students can also look up official Thanaweya results by seating number and see their track rank among peers.

Results are **indicative only** — final placement is decided by Egypt's official coordination system.

## Features

### Public

| Feature                 | Route               | Description                                                          |
| ----------------------- | ------------------- | -------------------------------------------------------------------- |
| Landing page            | `/`                 | Overview and entry points to prediction and result lookup            |
| College prediction      | `/predict`          | Select track, enter total score, submit for eligibility check        |
| Prediction results      | `/results`          | Paginated list of eligible faculties, client-side search, load-more  |
| Thanaweya result lookup | `/thanaweya-result` | Look up a student's official result by seating number                |
| Track rank              | `/track-rank`       | Seating lookup with track rank, percentile, and position among peers |
| Coordination guide      | `/guide`            | FAQ about the admission coordination process                         |
| Developer profile       | `/designer`         | Site credits and developer information                               |

**Prediction behavior**

- Only faculties where `student score >= cutoff score` are returned (eligible colleges only).
- Cutoffs must match the student's track and the faculty's `AllowedTracks`.
- Results are sorted by closest match to the cutoff (smallest absolute difference first).
- Pagination: frontend sends `pageSize: 20`; API default is 10 and supports up to 100 per page.
- Search filters loaded results by university or faculty name (client-side).
- "Load more" and "Show all colleges" fetch remaining pages from the API.

**Thanaweya result lookup**

- Look up by seating number for the current admission year.
- Returns name, total score, student case, inferred track, and track rank when data is available.
- Track rank is computed among students in the same track: higher score ranks better; ties broken by lower seating number.

**Academic tracks**

| API value     | Description        |
| ------------- | ------------------ |
| `Science`     | Science stream     |
| `Mathematics` | Mathematics stream |
| `Literature`  | Literature stream  |

The public UI is RTL and displays Arabic labels; track values sent to the API use the English identifiers above.

### Admin

Admin access is not linked from the public site. Sign in directly at `/admin/login`.

| Page                   | Route                   | Description                                                                                                   |
| ---------------------- | ----------------------- | ------------------------------------------------------------------------------------------------------------- |
| Dashboard              | `/admin`                | Counts for governorates, universities, faculties, university-faculty pairs, student results                   |
| Cutoffs                | `/admin/cutoffs`        | CRUD for admission cutoffs (university/faculty dropdowns from existing catalog)                               |
| Import cutoffs         | `/admin/import`         | Import cutoffs from Markdown (`.md`) for one track at a time; replaces existing cutoffs for that year + track |
| Import student results | `/admin/import-results` | Import Thanaweya results from Excel (`.xlsx`) for a selected admission year                                   |

Import pages show a progress overlay during upload and processing. Leaving the page is blocked until the import finishes or you confirm navigation away.

**Default admin credentials (development only)**

- Email: `admin@tansekak.local`
- Password: `Admin@12345`

Production requires unique credentials via `AdminSeed__Email` and `AdminSeed__Password`. Dev defaults are rejected at startup in non-Development environments.

**API-only admin operations** (no UI — use API client or seed data):

- Governorate, university, faculty, and university-faculty CRUD
- Admission year CRUD and publish (`POST /api/admin/admission-years/{id}/publish`)

## Tech stack

| Layer              | Technology                                              |
| ------------------ | ------------------------------------------------------- |
| Backend            | ASP.NET Core 10, EF Core, PostgreSQL (Npgsql), Identity |
| Frontend           | Angular 19 (standalone components, RTL UI)              |
| Validation         | FluentValidation                                        |
| Excel import       | ClosedXML                                               |
| Large file storage | Cloudflare R2 (S3-compatible presigned uploads)         |
| Tests              | xUnit (unit tests in `tests/`)                          |
| CI                 | GitHub Actions (build + test)                           |
| Deployment         | Docker → Render web service                             |

## Architecture

Clean Architecture with four backend projects:

```
Tansekak.Domain          → Entities, enums (anemic POCO model)
Tansekak.Application     → DTOs, service interfaces, validators, business helpers
Tansekak.Infrastructure  → EF Core, Identity, service implementations, import parsers, seeding
Tansekak.Api             → Controllers, middleware, host
```

The Application layer uses **service interfaces + DTOs** (not CQRS/MediatR). Services inject `AppDbContext` directly — there is no repository layer.

The Angular SPA lives in `client/`. In production, the API serves the built frontend from `wwwroot/` and falls back to `index.html` for client-side routing.

Integer entity IDs are allocated application-side via `EntityIdAllocator` and the `EntityIdSequences` table (not PostgreSQL identity columns).

## Project structure

```
Tansekak/
├── client/                    # Angular 19 frontend
├── SeededData/                # JSON catalog seed (copied to output as SeedData/)
├── docs/
│   ├── PRD.md                 # Original product requirements
│   └── PRODUCTION_SETUP.md    # Deploy checklist and env vars
├── .github/workflows/main.yml # CI (build + test)
├── Dockerfile                 # Multi-stage build (Node + .NET)
├── tests/
│   ├── Tansekak.Application.Tests/
│   └── Tansekak.Infrastructure.Tests/
├── src/
│   ├── Tansekak.Api/
│   ├── Tansekak.Application/
│   ├── Tansekak.Domain/
│   └── Tansekak.Infrastructure/
└── Tansekak.sln
```

## Prerequisites

**Local development**

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- PostgreSQL 16+ (local install, or a [Neon](https://neon.tech) dev database)

**Production**

- [Neon](https://neon.tech) Postgres database
- [Render](https://render.com) web service
- [Cloudflare R2](https://developers.cloudflare.com/r2/) bucket for large Excel imports (>20 MB)

## Quick start (local)

### 1. Database

Ensure PostgreSQL is running and update the connection string in `src/Tansekak.Api/appsettings.Development.json` if needed (default: `localhost:5432`, database `Tansekak`, user `postgres`).

### 2. API

```powershell
cd src/Tansekak.Api
dotnet run
```

- URL: `http://localhost:5080`
- OpenAPI (Development only): `http://localhost:5080/openapi/v1.json` (JSON spec only — no Swagger UI)
- Health: `http://localhost:5080/health`
- On startup: applies EF migrations, seeds catalog if empty, creates bootstrap admission year and dev admin user

### 3. Frontend

In a second terminal:

```powershell
cd client
npm install
npm start
```

- URL: `http://localhost:4200`
- API requests are proxied to `http://localhost:5080` via `client/proxy.conf.json`

### 4. First-run data (required for predictions)

After a fresh database bootstrap:

1. Sign in at `/admin/login` with the dev credentials above
2. Import cutoff Markdown files for each track via `/admin/import`
3. Optionally import student results via `/admin/import-results`

Predictions return no results until cutoffs are imported for the current year.

## Production deploy (Neon + Render + Cloudflare R2)

**Setup guide:** [docs/PRODUCTION_SETUP.md](docs/PRODUCTION_SETUP.md) (step-by-step checklist and environment variables)

Stack:

| Layer     | Provider   | Role                                            |
| --------- | ---------- | ----------------------------------------------- |
| App + SPA | Render     | Docker web service (API + Angular in `wwwroot`) |
| Database  | Neon       | Managed PostgreSQL                              |
| Storage   | Cloudflare | R2 bucket for large Excel imports (>20 MB)      |

### Quick deploy steps

1. Create a Neon project and copy the pooled connection string
2. Connect the GitHub repo to Render and create a Docker web service from `./Dockerfile`
3. Set secret env vars (see [Environment variables](docs/PRODUCTION_SETUP.md#environment-variables) in the setup guide)
4. After first boot, import cutoffs for all three tracks via admin
5. Verify `/health`, SPA routes, admin login, and prediction

GitHub Actions runs CI only (build + test). Render auto-deploys on push to `main`.

No `Frontend:Origin` CORS setting is needed — the SPA and API share the same origin.

**Plan note:** Prefer Render **Starter** plan for always-on hosting. Free tier spin-down can interrupt large imports.

## Configuration

### Connection string

Local default is in `src/Tansekak.Api/appsettings.Development.json`:

```
Host=localhost;Port=5432;Database=Tansekak;Username=postgres;Password=postgres
```

Base `appsettings.json` has an empty `DefaultConnection`. Production accepts either:

- `ConnectionStrings__DefaultConnection` — Npgsql keyword format
- `DATABASE_URL` — Neon-style `postgresql://...` URI (converted automatically with `SslMode=Require`)

`DatabaseConnectionResolver` picks the first configured value and normalizes PostgreSQL URIs to Npgsql keyword form.

### App settings

| Key                  | Purpose                                                                   |
| -------------------- | ------------------------------------------------------------------------- |
| `Tansekak:AppName`   | Application name returned by `/api/config`                                |
| `AdminSeed:Email`    | Admin user email for first-run seed                                       |
| `AdminSeed:Password` | Admin user password for first-run seed                                    |
| `Frontend:Origin`    | CORS origin (set in `appsettings.Development.json` for local Angular dev) |
| `R2:AccountId`       | Cloudflare account ID for large Excel imports                             |
| `R2:AccessKeyId`     | R2 S3-compatible access key                                               |
| `R2:SecretAccessKey` | R2 S3-compatible secret key                                               |
| `R2:BucketName`      | R2 bucket for temporary import files (default: `tansekak-imports`)        |

Environment variables use `__` as the nested separator (e.g. `R2__AccountId`).

### Production environment variables

| Variable | Required | Notes |
| -------- | -------- | ----- |
| `ConnectionStrings__DefaultConnection` | Yes | Neon pooled connection string (or use `DATABASE_URL`) |
| `AdminSeed__Email` | Yes | Unique email (not `admin@tansekak.local`) |
| `AdminSeed__Password` | Yes | Strong password (not `Admin@12345`) |
| `R2__AccountId` | For large imports | Cloudflare account ID |
| `R2__AccessKeyId` | For large imports | R2 S3 API access key |
| `R2__SecretAccessKey` | For large imports | R2 S3 API secret |
| `R2__BucketName` | Optional | Default: `tansekak-imports` |

On startup in non-Development environments, the app validates the connection string and admin seed credentials before running migrations. Missing R2 credentials log a warning only.

Do **not** set `Frontend__Origin` in production — the SPA and API share the same origin.

### Cloudflare R2 (large imports)

Student result imports **≤20 MB** upload directly to the API (synchronous). Files **>20 MB** use presigned URLs: the browser uploads to Cloudflare R2, then the API reads the object via a background job, imports it, and deletes it.

R2 is **optional** at startup (warning logged) but **required** for large Excel imports. Without R2 credentials, uploads over 20 MB return HTTP 503.

See [docs/PRODUCTION_SETUP.md](docs/PRODUCTION_SETUP.md) for bucket setup, API token, and CORS configuration.

#### Upload flow (>20 MB)

1. `POST /api/admin/admission-years/{yearId}/import-results/upload-url` — returns presigned PUT URL + `objectKey`
2. Browser `PUT` file bytes to R2
3. `POST /api/admin/admission-years/{yearId}/import-results/from-storage` — starts async import
4. `GET /api/admin/import-jobs/{jobId}` — poll until `status` is `completed` or `failed`

### Environment behavior

| Environment | Notes                                                                                    |
| ----------- | ---------------------------------------------------------------------------------------- |
| Development | OpenAPI at `/openapi/v1.json`, CORS for `http://localhost:4200`, relaxed cookie security |
| Production  | HSTS, secure cookies, production config validation, frontend served from `wwwroot/`      |

## Database

### Entities

- **Governorate** — Egyptian governorates (`NameAr`)
- **University** — Public universities and institutes (`Public`, `Institute`)
- **Faculty** — Faculty names with `AllowedTracks` (JSON list of academic tracks)
- **UniversityFaculty** — Links a university to a faculty
- **AdmissionYear** — Admission cycle (year, max score, `IsCurrent` flag)
- **AdmissionCutoff** — Minimum score per year, university-faculty, and track
- **StudentResult** — Imported Thanaweya results (seating number, name, score, case, inferred track)
- **ImportJob** — Async import job tracking (Guid PK, status, counts)
- **EntityIdSequence** — Manual integer ID allocation counters

### Seeding

JSON files in [SeededData/](SeededData/) are copied to the API output directory as `SeedData/` and read from disk at startup:

| File                       | Content                  |
| -------------------------- | ------------------------ |
| `Governorates.json`        | Governorate catalog      |
| `Universities.json`        | University catalog       |
| `Faculties.json`           | Faculty catalog          |
| `UniversityFaculties.json` | University–faculty links |

**Seeding runs exactly once** when `Governorates` has zero rows. After that, the database is the sole source of truth managed through admin APIs.

On first bootstrap, the app also creates:

- One **AdmissionYear** for the current UTC calendar year (max score 320, marked current)
- Dev admin user (or production admin from `AdminSeed__*`)

**Cutoffs are not seeded.** Import them via admin after first boot.

### Migrations

EF Core migrations live in `src/Tansekak.Infrastructure/Persistence/Migrations/`. They are applied automatically on startup.

## API reference

All controller responses use the envelope `{ success, message, data, errors? }`.

`GET /health` returns raw JSON `{ "status": "healthy" }` (not wrapped).

Authentication is **cookie-based** ASP.NET Core Identity (not JWT). Admin endpoints require the `Administrator` role.

### Public

| Method | Path                                 | Description                               |
| ------ | ------------------------------------ | ----------------------------------------- |
| `GET`  | `/api/config`                        | Current year, max score, available tracks |
| `POST` | `/api/admission/predict`             | Predict eligible faculties                |
| `GET`  | `/api/thanaweya-results/{seatingNo}` | Look up student result by seating number  |

**Predict request body**

```json
{
  "track": "Science",
  "score": 300,
  "page": 1,
  "pageSize": 20
}
```

**Predict response**

```json
{
  "success": true,
  "data": {
    "results": [
      {
        "university": { "nameAr": "..." },
        "faculty": { "nameAr": "..." }
      }
    ],
    "hasMore": true,
    "totalCount": 142
  }
}
```

### Admin (requires `Administrator` role, cookie auth)

| Method                | Path                                                              | Description                                                 |
| --------------------- | ----------------------------------------------------------------- | ----------------------------------------------------------- |
| `POST`                | `/api/admin/auth/login`                                           | Sign in                                                     |
| `POST`                | `/api/admin/auth/logout`                                          | Sign out                                                    |
| `GET`                 | `/api/admin/auth/me`                                              | Current user                                                |
| `GET`                 | `/api/admin/dashboard`                                            | Dashboard stats                                             |
| `GET/POST/PUT`        | `/api/admin/governorates`                                         | Governorate CRUD (no delete)                                |
| `GET/POST/PUT`        | `/api/admin/universities`                                         | University CRUD (no delete)                                 |
| `GET/POST/PUT`        | `/api/admin/faculties`                                            | Faculty CRUD (no delete)                                    |
| `GET/POST/PUT`        | `/api/admin/university-faculties`                                 | University–faculty CRUD (no delete)                         |
| `GET/POST/PUT`        | `/api/admin/admission-years`                                      | Admission year CRUD                                         |
| `GET`                 | `/api/admin/admission-years/current`                              | Current admission year                                      |
| `POST`                | `/api/admin/admission-years/{id}/publish`                         | Set year as current                                         |
| `GET/POST/PUT/DELETE` | `/api/admin/admission-cutoffs`                                    | Cutoff CRUD (paginated)                                     |
| `POST`                | `/api/admin/admission-years/{yearId}/import`                      | Import cutoffs from `.md`                                   |
| `POST`                | `/api/admin/admission-years/{yearId}/import-results`              | Import student results from `.xlsx` (direct upload, ≤20 MB) |
| `POST`                | `/api/admin/admission-years/{yearId}/import-results/upload-url`   | Get presigned R2 upload URL (large files)                   |
| `POST`                | `/api/admin/admission-years/{yearId}/import-results/from-storage` | Start import from R2 object (large files)                   |
| `GET`                 | `/api/admin/import-jobs/{jobId}`                                  | Poll async import job status                                |

## Import formats

### Cutoff import (Markdown)

- File extension: `.md`
- Max size: 10 MB
- One track per upload (`Science`, `Mathematics`, or `Literature`)
- Replaces all existing cutoffs for the selected year and track
- Expected format: Markdown table with college name and cutoff score columns
- College names are matched to the university–faculty catalog using Arabic text normalization
- Faculty must allow the imported track in its `AllowedTracks`

### Student result import (Excel)

- File extension: `.xlsx`
- Max size: 100 MB (global request body limit)
- Required columns (header row, English only): `seating_no`, `arabic_name`, `total_degree`, `student_case_desc`
- Imported for the selected admission year; replaces all existing results for that year
- Track is inferred from student case description and/or seating number
- Student totals are not capped at the admission cutoff maximum score

**Upload paths**

| File size | Path                                                                                                |
| --------- | --------------------------------------------------------------------------------------------------- |
| ≤20 MB    | Direct `POST /import-results` (multipart upload to API, synchronous)                                |
| >20 MB    | Presigned R2 upload + async job — requires [Cloudflare R2](#cloudflare-r2-large-imports) configured |

Large imports run asynchronously; poll `GET /api/admin/import-jobs/{jobId}` until complete.

## Building for production manually

```powershell
docker build -t tansekak .
docker run -p 8080:8080 `
  -e ConnectionStrings__DefaultConnection="Host=...;SSL Mode=Require" `
  -e AdminSeed__Email="admin@example.com" `
  -e AdminSeed__Password="your-strong-password" `
  tansekak
```

Or build locally without Docker:

```powershell
cd client
npm ci
npm run build -- --configuration production

dotnet publish src/Tansekak.Api/Tansekak.Api.csproj -c Release -o ./publish
Copy-Item -Path client/dist/client/browser/* -Destination publish/wwwroot -Recurse -Force
```

## Testing

```powershell
dotnet test Tansekak.sln --configuration Release
```

Unit tests cover Application helpers (track inference, matching rules, results query utils) and Infrastructure services (admission year publish, import jobs, database connection resolution). There are no integration or end-to-end test projects yet.

## Documentation

| Document                                             | Purpose                                                           |
| ---------------------------------------------------- | ----------------------------------------------------------------- |
| [README.md](README.md)                               | Primary developer and operator guide (this file)                  |
| [docs/PRODUCTION_SETUP.md](docs/PRODUCTION_SETUP.md) | Production deploy checklist, env vars, and troubleshooting        |
| [docs/PRD.md](docs/PRD.md)                           | Original product requirements (historical; see amendment section) |

## Disclaimer

Tansekak uses historical official cutoff data to estimate eligibility. It does not replace Egypt's official electronic coordination portal. Actual admission depends on demand, available seats, preference order, and official rules for the current cycle.
