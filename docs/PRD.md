# Tansekak — Product Specification (As-Built)

| Field | Value |
| --- | --- |
| Product | Tansekak (تنسيقك) |
| Status | Current stage — as built |
| Date | 2026-09-14 |
| Audience | Operators, engineers, and reviewers of the shipped product |

This document describes the **shipped** product. It is not a backlog or a wish list.

The original long-form requirements document is archived at [docs/PRD-ORIGINAL.md](PRD-ORIGINAL.md). Do not treat that file as the source of truth for current behavior.

Operator and developer setup lives in [README.md](../README.md). Production deploy details live in [docs/PRODUCTION_SETUP.md](PRODUCTION_SETUP.md).

---

## 1. Purpose

Tansekak is an **admission eligibility checker** for Egyptian Thanaweya Amma graduates.

A student enters an academic track and a total score. The app compares those values to official cutoff scores for the **current published admission year** and lists university faculties the student is **eligible** for.

Students can also look up an imported Thanaweya result by seating number and see their rank among peers in the same track.

Results are **indicative only**. Egypt’s official electronic coordination portal decides final placement. Tansekak does not register preferences, allocate seats, or issue admission decisions.

---

## 2. Users

| User | Goal |
| --- | --- |
| Student / parent | Check which faculties a score is eligible for; look up a Thanaweya result and track rank; read a short coordination FAQ. |
| Operator / administrator | Publish the current admission year; maintain cutoffs (CRUD and Markdown import); import student results from Excel. |
| Catalog maintainer | Manage governorates, universities, faculties, and university–faculty links via API only (no admin UI). |

There is a single Identity role: **Administrator**. Public visitors are unauthenticated. Admin login is not linked from the public site.

---

## 3. In scope

- Public RTL Arabic SPA for prediction, results, Thanaweya lookup, track rank, guide, and designer credits.
- Eligible-college prediction against the current published year.
- Admin cookie-authenticated UI for years, cutoffs, and imports.
- Catalog JSON bootstrap, admission-year bootstrap, and admin seed.
- Markdown cutoff import and Excel student-result import for a chosen admission year (UI always uses the current year).
- Direct Excel upload ≤20 MB; Cloudflare R2 + async job for larger files.
- Envelope API with stable `errorCode` values and Arabic failure messages.

---

## 4. Out of scope / non-goals

Tansekak does **not**:

- Replace the official coordination portal or submit student preferences.
- Guarantee placement, seats, or demand-adjusted cutoffs.
- Return ineligible faculties or extra eligibility bands.
- Expose Mathematics as a public track (علوم and رياضة are one Science bucket).
- Store English names, slugs, or active/inactive flags on catalog entities.
- Seed cutoffs automatically.
- Provide admin UI for governorates, universities, faculties, or university–faculties.
- Use bearer tokens for auth.
- Ship OpenAPI outside Development.
- Include `Tansekak.Api.Tests` in the solution or CI `dotnet test Tansekak.sln` run.
- Bundle sample cutoff Markdown into the Docker image.

---

## 5. Branding and UI

| Surface | Name |
| --- | --- |
| Public brand | Arabic **تنسيقك** |
| API `appName` (`GET /api/config`) | `tansekak` |
| Admin chrome | **لوحة الإدارة** |

- UI is **RTL Arabic**.
- Catalog and result names are **NameAr only**. There is no `NameEn`, `Slug`, or `IsActive`.
- Admin sign-in is at `/admin/login` and is not linked from public pages.

---

## 6. Public product

### 6.1 Routes

| Route | Page |
| --- | --- |
| `/` | Landing — overview and entry points |
| `/predict` | Track + score form |
| `/results` | Eligible faculties (paginated, searchable) |
| `/thanaweya-result` | Seating-number result lookup |
| `/track-rank` | Seating lookup with track rank among peers |
| `/guide` | Static coordination FAQ |
| `/designer` | Developer / credits page |

Unknown paths redirect to `/`.

### 6.2 Prediction

**Request:** academic track and total score. The API uses the **current published** admission year (`IsCurrent`). Score must not exceed that year’s `MaximumScore`.

**Eligibility (must all hold):**

1. Cutoff belongs to the current year.
2. Student score **≥** cutoff score (eligible only).
3. Cutoff track is in the student’s Science or Literature **bucket**.
4. Faculty `AllowedTracks` matches the student’s canonical track.

**Sort:** `abs(score − cutoff)` ascending (closest eligible cutoff first). Duplicate university–faculty rows are collapsed to one result.

**Pagination:**

| Layer | Page size |
| --- | --- |
| Frontend | `pageSize` **20**, unlimited load-more (and show-all remaining pages) |
| API | Default **10**, maximum **100** |

**Search:** client-side filter of **already loaded** rows by university or faculty `NameAr`. Search does not query the server.

Until cutoffs exist for the current year, prediction returns an empty list.

### 6.3 Thanaweya lookup and track rank

Both `/thanaweya-result` and `/track-rank` look up `GET /api/thanaweya-results/{seatingNo}` for the **current** admission year.

When a row exists, the API returns seating number, Arabic name, total degree, student case, inferred/canonical track, track rank, and track cohort size when rank can be computed.

**Rank rule:** among students in the same canonical track, **higher score ranks better**; ties are broken by **lower seating number**.

Lookup returns not found when the seating number is missing for the current year.

---

## 7. Academic tracks

Public UI and `GET /api/config` expose **two** tracks:

| API value | Arabic label |
| --- | --- |
| `Science` | الشعبة العلمية |
| `Literature` | الشعبة الأدبية |

The domain enum still has `Mathematics = 2`. Mathematics is **canonicalized to Science everywhere** (prediction, import, rank, labels, `AllowedTracks` matching).

The **Science bucket** includes stored `Science` and `Mathematics` cutoff/result rows. Literature is its own bucket.

---

## 8. Admin product

Authentication is **cookie-based ASP.NET Core Identity** with the **Administrator** role.

| Route | Capability |
| --- | --- |
| `/admin/login` | Sign in (not linked from public) |
| `/admin` | Dashboard |
| `/admin/years` | Create, edit, and **publish** (POST publish) |
| `/admin/cutoffs` | CRUD for the **current year only** (API may filter by `yearId`) |
| `/admin/import` | Science and/or Literature `.md` for the **current year** |
| `/admin/import-results` | `.xlsx` for the **current year**; replaces all results for that year |

Import pages show a progress overlay. Navigation away is **blocked** until the import finishes or the operator confirms leaving.

### 8.1 Dashboard

Shows the current year plus counts for:

- governorates
- universities
- faculties
- university–faculties
- student results

The dashboard API also returns `cutoffsCount`. The UI **does not display** it.

### 8.2 Admission years

- Create and edit year number and maximum score.
- Year range: **2000–2100**.
- Maximum score: **0.01–1000**, default **320**.
- Duplicate calendar year is rejected.
- **Publish** (`POST /api/admin/admission-years/{id}/publish`) marks one year `IsCurrent` and clears the flag on others.
- Prediction, public lookup, and the admin import UI all use the published current year.

### 8.3 Cutoffs

Admin UI lists/creates/updates/deletes cutoffs for the current year. University and faculty dropdowns come from the existing catalog. Cutoffs are the only catalog-adjacent records the UI can delete.

### 8.4 API-only catalog (no UI)

These have admin API endpoints (GET/POST/PUT, no delete) and no SPA pages:

- Governorates
- Universities
- Faculties
- University–faculties

---

## 9. Data model (summary)

Business entities use **integer IDs** allocated by `EntityIdAllocator` / `EntityIdSequence`. ASP.NET Identity tables use **PostgreSQL identity columns**. `ImportJob` uses a **Guid** primary key.

| Entity | Role |
| --- | --- |
| **Governorate** | Egyptian governorate (`NameAr`) |
| **University** | `NameAr`, `GovernorateId`, `Type` = `Public` (1) or `Institute` (2) |
| **Faculty** | `NameAr`, `AllowedTracks` (JSON list of academic tracks) |
| **UniversityFaculty** | Links a university to a faculty; cutoffs hang off this pair |
| **AdmissionYear** | Calendar year, `MaximumScore`, `IsCurrent` |
| **AdmissionCutoff** | Year + university–faculty + track + `CutoffScore` |
| **StudentResult** | Year + seating no, Arabic name, total degree, case description, optional stored track |
| **ImportJob** | Async Excel import status (`queued` / `running` / `completed` / `failed`) |
| **EntityIdSequence** | Next integer ID per entity name |

**Relationships (high level):**

```
Governorate 1──* University 1──* UniversityFaculty *──1 Faculty
AdmissionYear 1──* AdmissionCutoff *──1 UniversityFaculty
AdmissionYear 1──* StudentResult
AdmissionYear 1──* ImportJob
```

Uniqueness that matters in operations: one calendar year value; cutoffs are replaced per year+track on Markdown import; student results are replaced per year on Excel import.

---

## 10. Bootstrap and seeding

On startup the API applies EF migrations, then:

1. **Catalog seed (once):** if `Governorates` is empty, load JSON from `SeededData/` (`Governorates`, `Universities`, `Faculties`, `UniversityFaculties`). After that, the database is the source of truth.
2. **Admission year:** create year **2027**, max score **320**, `IsCurrent = true`.
3. **Admin user:** from `AdminSeed` (`AdminSeed__Email` / `AdminSeed__Password`). Development defaults are rejected at startup outside Development.

**Cutoffs are not seeded.** Sample files:

- `SeededData/cutoffs/science-2026.md`
- `SeededData/cutoffs/literature-2026.md`

**2026** is the official source cycle in those files. The operator attaches them to the **CURRENT** year (bootstrap default **2027**) via `/admin/import`. Files are uploaded from a **local clone**; they are **not** in the Docker image.

Student workbooks are not stored in the repo.

---

## 11. Import

### 11.1 Cutoffs (Markdown)

- Pipe table; headers **الكلية** / **الحد الأدنى**.
- `.md` only, max **10 MB**, **one track per file**.
- UI slots: Science and/or Literature for the **current** year.
- Each file **replaces** all cutoffs for that year + that track.
- `Mathematics` on import is accepted and stored as **Science**.
- College labels are matched to the university–faculty catalog (Arabic normalization). Faculty must allow the imported track.

### 11.2 Student results (Excel)

- Columns: `seating_no`, `arabic_name`, `total_degree`, `student_case_desc`.
- UI always imports into the **current** year. API path includes `yearId` and can target any year.
- Import **replaces all** student results for that year.
- Track is inferred from case description and/or seating number, then canonicalized.

| Size | Path |
| --- | --- |
| ≤20 MB | Direct multipart POST, synchronous |
| >20 MB | Presigned R2 PUT, then `from-storage` async job |
| Direct upload >20 MB | HTTP **413** |
| Large upload without R2 | HTTP **503** |

Poll `GET /api/admin/import-jobs/{jobId}` until `completed` or `failed`. R2 is optional at process start (warning only) and required for the large-file path.

---

## 12. API contract

Envelope for controller JSON:

```json
{
  "success": true,
  "message": "Operation completed successfully.",
  "errorCode": null,
  "data": {},
  "errors": null
}
```

| Rule | Behavior |
| --- | --- |
| Success default `message` | English `Operation completed successfully.` |
| Failure `message` | Arabic from `ArabicErrorCatalog` |
| Failure `errorCode` | Stable machine code |
| `GET /health` | Raw `{ "status": "healthy" }` — **not** wrapped |
| Auth | Cookie Identity, not tokens |
| OpenAPI | `/openapi/v1.json` in **Development only** (no Swagger UI) |

### 12.1 Public endpoints

| Method | Path | Purpose |
| --- | --- | --- |
| `GET` | `/api/config` | `appName`, current year, max score, tracks `Science` / `Literature` |
| `POST` | `/api/admission/predict` | Eligible faculties (`track`, `score`, `page`, `pageSize`) |
| `GET` | `/api/thanaweya-results/{seatingNo}` | Current-year student result + rank fields |

### 12.2 Admin endpoints (Administrator cookie)

| Area | Methods / notes |
| --- | --- |
| Auth | `POST /api/admin/auth/login`, `logout`; `GET /api/admin/auth/me` |
| Dashboard | `GET /api/admin/dashboard` |
| Catalog | `GET/POST/PUT` governorates, universities, faculties, university-faculties |
| Years | `GET/POST/PUT /api/admin/admission-years`; `GET .../current`; `POST .../{id}/publish` |
| Cutoffs | `GET/POST/PUT/DELETE /api/admin/admission-cutoffs` (optional `yearId` filter) |
| Cutoff import | `POST /api/admin/admission-years/{yearId}/import` |
| Results import | `POST .../import-results`; `.../upload-url`; `.../from-storage` |
| Jobs | `GET /api/admin/import-jobs/{jobId}` |

In production the API serves the Angular build from `wwwroot/` and falls back to `index.html` for client routes. Local Development uses CORS to `http://localhost:4200`. Production SPA and API share one origin.

---

## 13. Stack and architecture

| Layer | Technology |
| --- | --- |
| API | ASP.NET Core 10 |
| Data | EF Core, PostgreSQL, Npgsql |
| Frontend | Angular 19 standalone, RTL |
| Validation | FluentValidation |
| Excel | ClosedXML |
| Object storage | Cloudflare R2 (optional; large Excel) |
| Tests | xUnit |

**Clean Architecture** projects: `Tansekak.Domain`, `Tansekak.Application`, `Tansekak.Infrastructure`, `Tansekak.Api`.

Application layer is **service interfaces + DTOs**. There is **no MediatR** and **no repository layer**. Infrastructure services use `AppDbContext` directly. Domain entities are anemic POCOs.

---

## 14. Deployment

- **One Docker monolith** on Render (API + SPA). Bind HTTP to **`0.0.0.0:$PORT`**.
- **Neon** PostgreSQL.
- **Cloudflare R2** optional except for Excel files over 20 MB.
- Filesystem is ephemeral; do not rely on local disk beyond the image.
- GitHub Actions CI: build + `dotnet test Tansekak.sln`. Render deploys on push to `main`.

Production requires a real connection string and unique `AdminSeed` credentials (dev defaults are rejected).

---

## 15. Tests

| Project | In `Tansekak.sln` / CI | Coverage |
| --- | --- | --- |
| `Tansekak.Application.Tests` | Yes | Track rules, Arabic error catalog, related helpers |
| `Tansekak.Infrastructure.Tests` | Yes | Prediction, year publish, import, seeded Markdown parse, connection resolution |
| `Tansekak.Api.Tests` | **No** | Exists on disk (`GlobalExceptionHandler`); **not** in the solution, so `dotnet test Tansekak.sln` does not run it |

There are no integration or end-to-end test projects.

---

## 16. Success criteria (current stage)

The product is successful for this stage when:

- A student can get an **eligible-only** faculty list for the published year, filtered by track and `AllowedTracks`, sorted by closest cutoff.
- A student can look up an imported result by seating number and see track rank when data exists.
- An operator can publish a year, import Science/Literature Markdown, import Excel results, and CRUD current-year cutoffs.
- Failures return Arabic catalog messages and stable error codes; health is a raw probe; auth is cookies.
- Fresh databases bootstrap catalog + 2027 current year + admin, and predictions stay empty until cutoffs are imported.

---

## 17. Related documents

| Document | Role |
| --- | --- |
| [docs/PRD-ORIGINAL.md](PRD-ORIGINAL.md) | Archived original PRD (historical; not as-built) |
| [README.md](../README.md) | Developer and operator guide |
| [docs/API.md](API.md) | Public and admin HTTP contract |
| [docs/IMPORT.md](IMPORT.md) | Cutoff Markdown and student Excel import |
| [docs/PRODUCTION_SETUP.md](PRODUCTION_SETUP.md) | Production env, Render, Neon, R2 |
| [docs/production.env.example](production.env.example) | Production environment template |
