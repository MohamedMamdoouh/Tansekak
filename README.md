# Tansekak

**Vibe coded project** — built iteratively with AI-assisted development.

Tansekak is an admission eligibility checker for Egyptian Thanaweya Amma (high school) graduates. Students enter their academic track and total score; the app compares those values against official 2026 cutoff scores and shows which university faculties they are likely eligible for.

Results are **indicative only** — final placement is decided by Egypt's official coordination system.

## Features

### Public

| Feature                 | Route               | Description                                                         |
| ----------------------- | ------------------- | ------------------------------------------------------------------- |
| Landing page            | `/`                 | Overview and entry points to prediction and result lookup           |
| College prediction      | `/predict`          | Select track, enter total score, submit for eligibility check       |
| Prediction results      | `/results`          | Paginated list of eligible faculties, client-side search, load-more |
| Thanaweya result lookup | `/thanaweya-result` | Look up a student's official result by seating number               |
| Coordination guide      | `/guide`            | FAQ about the admission coordination process                        |

**Prediction behavior**

- Only faculties where `student score >= cutoff score` are returned (eligible colleges only).
- Results are sorted by closest match to the cutoff (smallest absolute difference first).
- Pagination: 20 results per page on the frontend; API supports up to 100 per page.
- Search filters loaded results by university or faculty name (client-side).
- "Load more" and "Show all colleges" fetch remaining pages from the API.

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
| Dashboard              | `/admin`                | Counts for governorates, universities, faculties, cutoffs, student results                                    |
| Cutoffs                | `/admin/cutoffs`        | CRUD for admission cutoffs (university/faculty dropdowns from existing catalog)                               |
| Import cutoffs         | `/admin/import`         | Import cutoffs from Markdown (`.md`) for one track at a time; replaces existing cutoffs for that year + track |
| Import student results | `/admin/import-results` | Import Thanaweya results from Excel (`.xlsx`) for a selected admission year                                   |

**Default admin credentials (development seed)**

- Email: `admin@tansekak.local`
- Password: `Admin@12345`

Override these in production via `AdminSeed:Email` and `AdminSeed:Password`.

## Tech stack

| Layer            | Technology                                                  |
| ---------------- | ----------------------------------------------------------- |
| Backend          | ASP.NET Core 10, EF Core, SQL Server, ASP.NET Core Identity |
| Frontend         | Angular 19 (standalone components, RTL UI)                  |
| Validation       | FluentValidation                                            |
| Excel import     | ClosedXML                                                   |
| Tests            | xUnit (unit + integration)                                  |
| Deployment       | GitHub Actions → MonsterASP.NET                             |

## Architecture

Clean Architecture with four backend projects:

```
Tansekak.Domain          → Entities, enums
Tansekak.Application     → DTOs, interfaces, shared helpers
Tansekak.Infrastructure  → EF Core, Identity, services, import parsers, seeding
Tansekak.Api             → Controllers, middleware, host
```

The Angular SPA lives in `client/`. In production, the API serves the built frontend from `wwwroot/` and falls back to `index.html` for client-side routing.

## Project structure

```
Tansekak/
├── client/                    # Angular 19 frontend
├── SeededData/                # Initial JSON seed (catalog + 2026 cutoffs)
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
- SQL Server 2022+ (local install or SQL Server Express)

**Production**

- [MonsterASP.NET](https://www.monsterasp.net/) account (website + MSSQL database)
- [Cloudflare R2](https://developers.cloudflare.com/r2/) bucket for large Excel imports (>20 MB)

## Quick start (local)

### 1. API

```powershell
cd src/Tansekak.Api
dotnet run
```

- URL: `http://localhost:5080`
- OpenAPI (Development only): `http://localhost:5080/openapi/v1.json`
- On startup: applies EF migrations and seeds the database from embedded `SeededData/` JSON if empty

### 2. Frontend

In a second terminal:

```powershell
cd client
npm install
npm start
```

- URL: `http://localhost:4200`
- API requests are proxied to `http://localhost:5080` via `client/proxy.conf.json`

## Production deploy (MonsterASP + Cloudflare R2)

**Setup guides:** [docs/PRODUCTION_SETUP.md](docs/PRODUCTION_SETUP.md) (step-by-step checklist) · [docs/production.env.example](docs/production.env.example) (environment variable template)

Stack:

| Layer    | Provider    | Role                                      |
| -------- | ----------- | ----------------------------------------- |
| App + SPA | MonsterASP | Single IIS site (API + Angular in `wwwroot`) |
| Database | MonsterASP | Managed MSSQL 2019/2022/2025              |
| Storage  | Cloudflare  | R2 bucket for large Excel imports (>20 MB) |

### 1. MonsterASP (website + database)

1. Create a **Website** in the [MonsterASP control panel](https://www.monsterasp.net/).
2. Create an **MSSQL database**: Control Panel → **Databases** → **Add database** → choose **MSSQL**.
3. Copy the MSSQL connection string from the control panel.
4. Enable **Let's Encrypt HTTPS** on the website.
5. Activate **WebDeploy** and download the `.publishSettings` profile (for Visual Studio or GitHub Actions).

Set environment variables under **Websites → Manage website → Scripting → Environment Variables**:

| Variable                               | Value                              |
| -------------------------------------- | ---------------------------------- |
| `ConnectionStrings__DefaultConnection` | MonsterASP MSSQL connection string |
| `ASPNETCORE_ENVIRONMENT`               | `Production`                       |
| `AdminSeed__Email`                     | Admin login email                  |
| `AdminSeed__Password`                  | Strong admin password              |
| `R2__AccountId`                        | Cloudflare account ID              |
| `R2__AccessKeyId`                      | R2 S3 API access key               |
| `R2__SecretAccessKey`                  | R2 S3 API secret                   |
| `R2__BucketName`                       | `tansekak-imports`                 |

No `Frontend:Origin` CORS setting is needed — the SPA and API share the same origin.

**Plan note:** Prefer a paid MonsterASP plan for always-on hosting. Free tier app pool idle timeout can interrupt large imports.

### 2. Publish and deploy

Build and publish manually, then deploy via **Visual Studio WebDeploy** (import the `.publishSettings` profile from the MonsterASP control panel):

```powershell
# Frontend
cd client
npm ci
npm run build -- --configuration production

# API — self-contained win-x86 for MonsterASP IIS (or use the MonsterASP publish profile)
dotnet publish src/Tansekak.Api/Tansekak.Api.csproj -c Release -o ./publish --runtime win-x86 --self-contained true
Copy-Item -Path client/dist/client/browser/* -Destination publish/wwwroot -Recurse -Force
```

Alternatively, from Visual Studio: right-click **Tansekak.Api** → **Publish** → **MonsterASP** profile, then copy the frontend into `publish/wwwroot`.

Publish `./publish` to your MonsterASP website using the WebDeploy profile.

### 3. Verify deployment

1. Open `https://<your-domain>/health` — should return `{ "status": "healthy" }`.
2. Browse SPA routes (`/predict`, `/admin/login`) to confirm `index.html` fallback works.
3. Sign in to the admin panel and confirm cookie auth works.

### 4. Cloudflare R2 (large imports)

See [Cloudflare R2 (large imports)](#cloudflare-r2-large-imports) below. R2 is required only for student Excel files **over 20 MB**.

## Configuration

### Connection string

`src/Tansekak.Api/appsettings.json`:

```
Server=localhost,1433;Database=Tansekak;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True
```

Production (MonsterASP) overrides via `ConnectionStrings__DefaultConnection`.

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
| `R2:BucketName`      | R2 bucket for temporary import files                                      |

Environment variables use `__` as the nested separator (e.g. `R2__AccountId`).

### Cloudflare R2 (large imports)

Student result imports **≤20 MB** upload directly to the API. Files **>20 MB** use presigned URLs: the browser uploads to Cloudflare R2, then the API reads the object, imports it, and deletes it.

R2 is **optional** for small files but **required** for large Excel imports. Without R2 credentials, uploads over 20 MB return HTTP 503.

#### 1. Create bucket

In [Cloudflare Dashboard](https://dash.cloudflare.com) → **R2 Object Storage** → **Create bucket**:

- Name: `tansekak-imports` (or your preferred name — set `R2__BucketName` to match)
- Copy your **Account ID** from the R2 overview page → `R2__AccountId`

No public bucket access is needed.

#### 2. Create API token

**R2 → Manage R2 API Tokens → Create API Token**:

| Setting     | Value                                                |
| ----------- | ---------------------------------------------------- |
| Permissions | **Object Read & Write** scoped to `tansekak-imports` |
| TTL         | No expiry (or rotate periodically)                   |

Save the **Access Key ID** → `R2__AccessKeyId` and **Secret Access Key** → `R2__SecretAccessKey` (shown once).

#### 3. Configure bucket CORS

Large uploads send a cross-origin **PUT** from the browser to `*.r2.cloudflarestorage.com`. In **R2 → your bucket → Settings → CORS policy**, add:

```json
[
  {
    "AllowedOrigins": [
      "https://<your-monsterasp-domain>",
      "http://localhost:4200"
    ],
    "AllowedMethods": ["PUT"],
    "AllowedHeaders": ["Content-Type"],
    "ExposeHeaders": ["ETag"],
    "MaxAgeSeconds": 3600
  }
]
```

Replace `<your-monsterasp-domain>` with your MonsterASP site URL (e.g. `https://yoursite.monsterasp.net`). Keep `http://localhost:4200` when testing large imports via `npm start` (the PUT does not go through the API proxy).

Optional: add a lifecycle rule to delete objects under prefix `imports/` after 1 day to clean up orphaned uploads.

#### Upload flow (>20 MB)

1. `POST /api/admin/admission-years/{yearId}/import-results/upload-url` — returns presigned PUT URL + `objectKey`
2. Browser `PUT` file bytes to R2
3. `POST /api/admin/admission-years/{yearId}/import-results/from-storage` — starts async import
4. `GET /api/admin/import-jobs/{jobId}` — poll until `status` is `completed` or `failed`

### Environment behavior

| Environment | Notes                                                                      |
| ----------- | -------------------------------------------------------------------------- |
| Development | OpenAPI enabled, CORS for `http://localhost:4200`, relaxed cookie security |
| Production  | HSTS, secure cookies, frontend served from `wwwroot/`                      |

## Database

### Entities

- **Governorate** — Egyptian governorates
- **University** — Public universities and institutes (`Public`, `Institute`)
- **Faculty** — Faculty names (shared catalog)
- **UniversityFaculty** — Links a university to a faculty
- **AdmissionYear** — Admission cycle (year, max score, `IsCurrent` flag)
- **AdmissionCutoff** — Minimum score per year, university-faculty, and track
- **StudentResult** — Imported Thanaweya results (seating number, name, score, case)

### Seeding

Seed files in `[SeededData/](SeededData/)` are linked into the Infrastructure assembly as embedded resources and read by filename:

| File                        | Content                                                 |
| --------------------------- | ------------------------------------------------------- |
| `Governorates.json`         | Governorate catalog                                     |
| `Universities.json`         | University catalog                                      |
| `Faculties.json`            | Faculty catalog                                         |
| `UniversityFaculties.json`  | University–faculty links                                |
| `AdmissionYears.json`       | Admission years (2026 seeded as current, max score 320) |
| `AdmissionCutoffs2026.json` | Cutoff scores for 2026                                  |

Seeding runs once when `AdmissionCutoffs` is empty. If partial data exists, business tables are cleared and re-seeded.

### Migrations

EF Core migrations live in `src/Tansekak.Infrastructure/Persistence/Migrations/`. They are applied automatically on startup.

## API reference

All responses use the envelope `{ success, message, data, errors? }`.

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
| `GET/POST/PUT`        | `/api/admin/governorates`                                         | Governorate CRUD                                            |
| `GET/POST/PUT`        | `/api/admin/universities`                                         | University CRUD                                             |
| `GET/POST/PUT`        | `/api/admin/faculties`                                            | Faculty CRUD                                                |
| `GET/POST/PUT`        | `/api/admin/university-faculties`                                 | University–faculty CRUD                                     |
| `GET/POST/PUT/PATCH`  | `/api/admin/admission-years`                                      | Admission year CRUD + publish                               |
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

### Student result import (Excel)

- File extension: `.xlsx`
- Max size: 100 MB
- Required columns (header row, English only): `seating_no`, `arabic_name`, `total_degree`, `student_case_desc`
- Imported for the selected admission year
- Student totals are not capped at the admission cutoff maximum score

**Upload paths**

| File size | Path                                                                                                  |
| --------- | ----------------------------------------------------------------------------------------------------- |
| ≤20 MB    | Direct `POST /import-results` (multipart upload to API)                                               |
| >20 MB    | Presigned R2 upload — requires [Cloudflare R2](#cloudflare-r2-large-imports) configured on the server |

Large imports run asynchronously; poll `GET /api/admin/import-jobs/{jobId}` until complete.

## Building for production manually

```powershell
# Frontend
cd client
npm ci
npm run build -- --configuration production

# API — self-contained win-x86 for MonsterASP IIS
dotnet publish src/Tansekak.Api/Tansekak.Api.csproj -c Release -o ./publish --runtime win-x86 --self-contained true
Copy-Item -Path client/dist/client/browser/* -Destination publish/wwwroot -Recurse -Force
```

## Disclaimer

Tansekak uses historical official cutoff data to estimate eligibility. It does not replace Egypt's official electronic coordination portal. Actual admission depends on demand, available seats, preference order, and official rules for the current cycle.
