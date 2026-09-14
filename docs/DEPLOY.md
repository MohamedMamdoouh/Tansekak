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

On boot the app runs migrations, seeds the catalog from JSON, creates admission year **2027** (max 320), and creates the admin user from `AdminSeed__*`.

**Cutoffs are not seeded.** From a local clone:

1. Sign in at `/admin/login`.
2. Confirm the year on `/admin/years`.
3. Upload `SeededData/cutoffs/science-2026.md` and `literature-2026.md` at `/admin/import`.

The `2026` in filenames is the official source cycle; rows attach to the current year (**2027** on bootstrap).

---

## 4. R2 (large Excel only)

Skip this section if all imports are ≤ 20 MB.

1. Create bucket `tansekak-imports` in [Cloudflare R2](https://dash.cloudflare.com).
2. Create an API token with **Object Read & Write** on that bucket.
3. Set `R2__AccountId`, `R2__AccessKeyId`, `R2__SecretAccessKey` on Render (`R2__BucketName` defaults to `tansekak-imports`).
4. Add bucket CORS so the browser can PUT uploads:

```json
[
  {
    "AllowedOrigins": ["https://<your-render-domain>.onrender.com", "http://localhost:4200"],
    "AllowedMethods": ["PUT"],
    "AllowedHeaders": ["Content-Type"],
    "ExposeHeaders": ["ETag"],
    "MaxAgeSeconds": 3600
  }
]
```

Replace `<your-render-domain>` exactly (include `https://`).

---

## 5. Smoke test

| Check | Expected |
| --- | --- |
| `GET /health` | `{ "status": "healthy" }` |
| `/predict` | SPA loads |
| `/admin/login` → sign in | Cookie auth works |
| `GET /api/config` | Current year, max score, tracks `Science` / `Literature` |
| `/admin/import` (after cutoffs uploaded) | Prediction returns results |
