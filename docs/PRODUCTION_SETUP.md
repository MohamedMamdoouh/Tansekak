# Production setup checklist

Use this checklist when deploying Tansekak to **Render** with **Neon Postgres** and **Cloudflare R2** storage.

See also: [production.env.example](./production.env.example) for all required environment variables.

---

## 1. Neon (PostgreSQL database)

- [ ] Create a project at [neon.tech](https://neon.tech)
- [ ] Copy the **pooled** connection string (Npgsql format, SSL enabled)
- [ ] Save it for Render as `ConnectionStrings__DefaultConnection`

Example format:

```
Host=ep-xxx.region.aws.neon.tech;Database=neondb;Username=...;Password=...;SSL Mode=Require
```

Alternatively, set `DATABASE_URL` (`postgres://...`) on Render — the app parses it automatically.

---

## 2. Render (web service)

- [ ] Connect your GitHub repo to [Render](https://render.com)
- [ ] Create a **Web Service** from [render.yaml](../render.yaml) (Blueprint) or the Dashboard
- [ ] Set secret environment variables (see [production.env.example](./production.env.example))
- [ ] Confirm deploy succeeds and health check passes at `/health`
- [ ] Prefer the **Starter plan** if large Excel imports must not be interrupted by free-tier spin-down

### Environment variables

Render Dashboard → your service → **Environment**:

- [ ] `ConnectionStrings__DefaultConnection` — Neon pooled connection string
- [ ] `ASPNETCORE_ENVIRONMENT` — `Production`
- [ ] `AdminSeed__Email` — unique admin email (not `admin@tansekak.local`)
- [ ] `AdminSeed__Password` — strong password (not `Admin@12345`)
- [ ] `R2__AccountId` — Cloudflare account ID
- [ ] `R2__AccessKeyId` — R2 S3 API access key
- [ ] `R2__SecretAccessKey` — R2 S3 API secret
- [ ] `R2__BucketName` — e.g. `tansekak-imports`

Do **not** set `Frontend__Origin` — SPA and API are same-origin on Render.

On first boot: EF migrations run, reference data seeds, admin user is created.

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

CI runs on every push and pull request to `main`:

- Builds Angular frontend
- Builds and tests .NET backend

Deploy is handled by Render auto-deploy on push to `main` (configured in [render.yaml](../render.yaml)).

---

## 5. Deploy

- [ ] Confirm Neon connection string and all Render env vars are set
- [ ] Push to `main` or trigger a manual deploy in Render
- [ ] Watch Render deploy logs for build success
- [ ] On first boot: EF migrations run, reference data seeds, admin user is created

---

## 6. Post-deploy verification

| Check | URL / action | Expected |
| ----- | ------------ | -------- |
| Health | `GET https://<domain>/health` | `{ "status": "healthy" }` |
| SPA routes | `https://<domain>/predict` | Angular app loads |
| Admin login | `https://<domain>/admin/login` | Login page loads |
| Auth | Sign in with `AdminSeed__*` credentials | Cookie auth over HTTPS |
| Config API | `GET https://<domain>/api/config` | JSON app config |
| Predict | Submit prediction form | `POST /api/admission/predict` succeeds |
| Small import | Upload Excel ≤ 20 MB in admin | Direct upload succeeds |
| Large import | Upload Excel > 20 MB in admin | Presigned URL flow completes |

---

## Troubleshooting

| Symptom | Likely cause |
| ------- | -------------- |
| App fails to start | Missing or invalid env vars — check Render deploy logs |
| `localhost` connection error | `ConnectionStrings__DefaultConnection` not set or still pointing locally |
| Admin login fails | Wrong `AdminSeed__*` values; user already created on first boot with different password |
| Large import returns 503 | R2 env vars missing or incomplete |
| Large import CORS error | R2 CORS `AllowedOrigins` does not exactly match your Render domain |
| Import job interrupted | Free-tier spin-down — upgrade to Starter plan or retry |
