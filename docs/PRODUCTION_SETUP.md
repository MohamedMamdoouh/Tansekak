# Production setup checklist

Use this checklist when deploying Tansekak to **Render** with **Neon Postgres** and **Cloudflare R2** storage.

See also: [production.env.example](./production.env.example) for environment variables, [IMPORT.md](IMPORT.md) for cutoff and student-result uploads, and [API.md](API.md) for the HTTP contract.

---

## Architecture

| Layer     | Provider   | Role                                              |
| --------- | ---------- | ------------------------------------------------- |
| App + SPA | Render     | Docker web service (API + Angular in `wwwroot`)   |
| Database  | Neon       | Managed PostgreSQL                                |
| Storage   | Cloudflare | R2 bucket for large Excel imports (>20 MB)        |

The app is a **monolith**: one container serves both `/api/*` and the Angular SPA. No CORS configuration is needed on Render.

---

## 1. Neon (PostgreSQL database)

- [ ] Create a project at [neon.tech](https://neon.tech)
- [ ] Copy the **pooled** connection string (Npgsql format, SSL enabled)
- [ ] Save it for Render as `ConnectionStrings__DefaultConnection`

Example format:

```
Host=ep-xxx.region.aws.neon.tech;Database=neondb;Username=...;Password=...;SSL Mode=Require
```

Neon's `postgresql://...` URI also works on either `ConnectionStrings__DefaultConnection` or `DATABASE_URL` — the app converts it to Npgsql keyword format with `SslMode=Require`.

---

## 2. Render (web service)

- [ ] Connect your GitHub repo to [Render](https://render.com)
- [ ] Create a **Web Service** → **Build from Dockerfile**
- [ ] Dockerfile path: `./Dockerfile`
- [ ] Docker context: repository root
- [ ] Health check path: `/health`
- [ ] Prefer the **Starter plan** if large Excel imports must not be interrupted by free-tier spin-down
- [ ] Set secret environment variables in the Render Dashboard (see [production.env.example](./production.env.example))
- [ ] Confirm deploy succeeds and health check passes at `/health`

### Environment variables

Render Dashboard → your service → **Environment**:

| Variable | Required | Notes |
| -------- | -------- | ----- |
| `ConnectionStrings__DefaultConnection` | Yes* | Neon pooled connection string |
| `DATABASE_URL` | Alternative | Neon `postgresql://…` URI if the connection string above is unset |
| `AdminSeed__Email` | Yes | Unique email (not `admin@tansekak.local`) |
| `AdminSeed__Password` | Yes | Strong password (not `Admin@12345`) |
| `R2__AccountId` | For large imports | Cloudflare account ID |
| `R2__AccessKeyId` | For large imports | R2 S3 API access key |
| `R2__SecretAccessKey` | For large imports | R2 S3 API secret |
| `R2__BucketName` | Optional | Default: `tansekak-imports` |
| `ASPNETCORE_ENVIRONMENT` | Optional | Already `Production` in Dockerfile |

Do **not** set `Frontend__Origin` — SPA and API are same-origin on Render.

\*Required unless `DATABASE_URL` is set.

`PORT` is set automatically by Render. The app binds to `0.0.0.0:$PORT` (default `8080`).

### Production startup validation

On first boot (and every restart), the app validates configuration **before** migrations:

- Connection string must be set and must **not** contain `localhost` or `127.0.0.1`
- `AdminSeed__Email` and `AdminSeed__Password` must be set
- Dev defaults (`admin@tansekak.local`, `Admin@12345`) are rejected
- Missing R2 credentials log a **warning** only (startup succeeds; large imports return HTTP 503)

If validation fails, check Render deploy logs for the exact error message.

### First boot sequence

1. EF Core migrations run automatically
2. Reference catalog seeds from `SeededData/` JSON (governorates, universities, faculties, links)
3. Bootstrap admission year is created (**2027**, max score 320, marked current)
4. Admin user is created from `AdminSeed__*` if no admin exists

**Important:** Cutoffs are **not** seeded and the Markdown files are **not** in the container (only catalog JSON is). After first deploy, clone the repo locally, sign in to admin, confirm the year on `/admin/years`, and upload `SeededData/cutoffs/science-2026.md` and `literature-2026.md` via `/admin/import`. Filename **2026** is the official source cycle; rows attach to the **current published year** (bootstrap **2027**). See [IMPORT.md](IMPORT.md).

---

## 3. Cloudflare R2 (large imports > 20 MB)

- [ ] [Cloudflare Dashboard](https://dash.cloudflare.com) → **R2 Object Storage** → Create bucket `tansekak-imports`
- [ ] Copy **Account ID** → `R2__AccountId`
- [ ] **Manage R2 API Tokens** → Create token with **Object Read & Write** scoped to the bucket
- [ ] Save Access Key ID → `R2__AccessKeyId` and Secret Key → `R2__SecretAccessKey`

### Bucket CORS (required for browser PUT uploads)

R2 → bucket → Settings → **CORS policy**:

```json
[
  {
    "AllowedOrigins": [
      "https://<your-render-domain>.onrender.com",
      "http://localhost:4200"
    ],
    "AllowedMethods": ["PUT"],
    "AllowedHeaders": ["Content-Type"],
    "ExposeHeaders": ["ETag"],
    "MaxAgeSeconds": 3600
  }
]
```

- [ ] Replace `<your-render-domain>` with your Render service URL (exact match, including `https://`)
- [ ] Keep `http://localhost:4200` when testing large imports via `npm start`
- [ ] Optional: lifecycle rule to delete objects under `imports/` after 1 day

---

## 4. GitHub Actions (CI)

CI runs on every push and pull request to `main` (`.github/workflows/main.yml`):

- Builds Angular frontend (production configuration)
- Builds and runs .NET unit tests on `Tansekak.sln`

CI does **not** build the Docker image or deploy. Deploy is handled by Render auto-deploy on push to `main`.

---

## 5. Deploy

- [ ] Confirm Neon connection string and all Render env vars are set
- [ ] Push to `main` or trigger a manual deploy in Render
- [ ] Watch Render deploy logs for build success
- [ ] From a **local clone**, sign in to admin, confirm the year on `/admin/years`, and upload Science + Literature Markdown from `SeededData/cutoffs/` (files are not on the server)

---

## 6. Post-deploy verification

| Check | URL / action | Expected |
| ----- | ------------ | -------- |
| Health | `GET https://<domain>/health` | `{ "status": "healthy" }` |
| SPA routes | `https://<domain>/predict` | Angular app loads |
| Admin login | `https://<domain>/admin/login` | Login page loads |
| Auth | Sign in with `AdminSeed__*` credentials | Cookie auth over HTTPS |
| Config API | `GET https://<domain>/api/config` | JSON with current year, max score, tracks `Science` and `Literature` |
| Admin years | `https://<domain>/admin/years` | Create / publish year works after login |
| Predict | Submit prediction form (after cutoff import) | `POST /api/admission/predict` returns results |
| Small import | Upload Excel ≤ 20 MB in admin | Direct upload succeeds |
| Large import | Upload Excel > 20 MB in admin | Presigned URL flow completes |

---

## Troubleshooting

| Symptom | Likely cause |
| ------- | -------------- |
| App fails to start on deploy | Missing env vars, localhost connection string, or dev admin credentials — check Render deploy logs |
| `ConnectionStrings:DefaultConnection must be configured` | `ConnectionStrings__DefaultConnection` not set on Render |
| `localhost` connection error | Connection string still points locally or not set |
| `AdminSeed:Email must not use the development default` | Still using `admin@tansekak.local` |
| Admin login fails | Wrong `AdminSeed__*` values; user already created on first boot with different password |
| Predict returns empty / service unavailable | No cutoffs imported yet, or no current admission year. Cutoff Markdown must be uploaded from a local clone; it is not in the Docker image |
| Large import returns 503 | R2 env vars missing or incomplete |
| Large import CORS error | R2 CORS `AllowedOrigins` does not exactly match your Render domain |
| Import job interrupted | Free-tier spin-down — upgrade to Starter plan or retry |
| Upload shows connection error (تعذر الاتصال بالخادم) | **Local:** API not running on `:5080`, frontend opened from `dist/` instead of `npm start`, or Postgres down. **Production:** Render service spun down or deploy failed — check `/health`. **Large Excel (>20 MB):** R2 PUT blocked by missing R2 config or CORS mismatch |
| OpenAPI not available | `/openapi/v1.json` is Development-only; production has no Swagger UI |
