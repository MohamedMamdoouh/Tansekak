# Deploy

Deploy Tansekak to **Render** (Docker monolith) with **Neon Postgres**. **Cloudflare R2** is optional — required only for Excel imports over 20 MB.

Related: [API.md](./API.md)

---

## Stack

| Layer | Provider | Notes |
| --- | --- | --- |
| App + SPA | Render | One container serves `/api/*` and the Angular app from `wwwroot` |
| Database | Neon | Pooled PostgreSQL connection string |
| Storage | Cloudflare R2 | Large Excel uploads only (> 20 MB) |

No CORS setup on Render — API and SPA share one origin. Do **not** set `Frontend__Origin`.

There is no `render.yaml`. Create the web service in the Render dashboard.

---

## 1. Neon

1. Create a project at [neon.tech](https://neon.tech).
2. Copy the **pooled** connection string.
3. Set it on Render as `ConnectionStrings__DefaultConnection` (or `DATABASE_URL`).

Npgsql keyword format or a `postgresql://…` URI both work. The app rejects `localhost` / `127.0.0.1` in production.

---

## 2. Render

1. Connect the GitHub repo at [render.com](https://render.com).
2. Create a **Web Service** → **Build from Dockerfile** (`./Dockerfile`, repo root).
3. Health check: `/health`.
4. Set environment variables in Render Dashboard → **Environment**:

   | Variable | Required | Notes |
   | --- | --- | --- |
   | `ConnectionStrings__DefaultConnection` | Yes* | Neon pooled connection string |
   | `DATABASE_URL` | Alternative | Neon `postgresql://…` URI if the connection string above is unset |
   | `AdminSeed__Email` | Yes | Not `admin@tansekak.local` |
   | `AdminSeed__Password` | Yes | Not `Admin@12345` |
   | `R2__AccountId` | For large imports | See section 4 |
   | `R2__AccessKeyId` | For large imports | |
   | `R2__SecretAccessKey` | For large imports | |
   | `R2__BucketName` | Optional | Default: `tansekak-imports` |

   \*Required unless `DATABASE_URL` is set. Missing R2 credentials only log a warning — startup still succeeds.

5. Push to `main` (auto-deploy) or trigger a manual deploy.
6. Use **Starter** plan if large imports must not be interrupted by free-tier spin-down.

`PORT` is set by Render. The app binds to `0.0.0.0:$PORT`.

---

## 3. After first deploy

On boot the app:

1. Rejects localhost DB strings and the development admin credentials.
2. Runs EF migrations.
3. Seeds the catalog from JSON if `Governorates` is empty, and creates admission year **2027** (max 320, current).
4. Repairs `Faculties.AllowedTracks` from seed JSON when they diverge.
5. Imports seed Markdown for any current-year track that has **zero** cutoffs (does not overwrite existing rows).
6. Creates the admin user from `AdminSeed__*`.

Catalog JSON is copied into the image as `SeedData/*.json`. Cutoff Markdown is copied by the API project when those files are in the Docker build context (`SeededData/cutoffs/*.md` → `SeedData/cutoffs/`). `.dockerignore` excludes `*.md` globally, so production images often skip cutoff bootstrap and log that seed cutoff files were not found.

If prediction is empty after first boot, sign in at `/admin/login`, confirm the year on `/admin/years`, then upload from a local clone at `/admin/import`:

| Track | Seed file |
| --- | --- |
| `Science` | `SeededData/cutoffs/science-2026.md` |
| `Mathematics` | `SeededData/cutoffs/mathematics-2026.md` |
| `Literature` | `SeededData/cutoffs/literature-2026.md` |

The `2026` in filenames is the official source cycle; rows attach to the current year (**2027** on bootstrap). Each import replaces that track only.

Student results are never seeded. Upload the Thanaweya Excel at `/admin/import-results` when you want lookup and track rank.

---

## 4. R2 (large Excel only)

Skip this section if all imports are ≤ 20 MB.

The admin UI uploads files over 20 MB to the API (same origin, max **100 MB**). The API streams the file to R2 and runs the existing async import job. **Browser CORS on the R2 bucket is not required** for `/admin/import-results`.

1. Create bucket `tansekak-imports` in [Cloudflare R2](https://dash.cloudflare.com).
2. Create an API token with **Object Read & Write** on that bucket.
3. Set `R2__AccountId`, `R2__AccessKeyId`, `R2__SecretAccessKey` on Render (`R2__BucketName` defaults to `tansekak-imports`).

Temp files during import live on Render’s ephemeral disk and are deleted when the job finishes. Durable data is Neon (including ASP.NET Data Protection keys) and R2 object storage.

---

## 5. Smoke test

| Check | Expected |
| --- | --- |
| `GET /health` | `{ "status": "healthy" }` |
| `/predict` | SPA loads |
| `/admin/login` → sign in | Cookie auth works |
| `GET /api/config` | Current year, max score, tracks `Science` / `Mathematics` / `Literature` |
| Predict after cutoffs exist | Science vs Mathematics with the same score return different faculty lists |
