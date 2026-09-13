# Production setup checklist

Use this checklist when deploying Tansekak to **MonsterASP** with **Cloudflare R2** storage.

See also: [production.env.example](./production.env.example) for all required environment variables.

---

## 1. MonsterASP (website + database)

- [ ] Create a **Website** in the [MonsterASP control panel](https://www.monsterasp.net/)
- [ ] Note your production URL (e.g. `https://yoursite.monsterasp.net`)
- [ ] Create an **MSSQL database**: Control Panel → **Databases** → **Add database** → **MSSQL**
- [ ] Copy the MSSQL **connection string** from the control panel
- [ ] Enable **Let's Encrypt HTTPS** on the website
- [ ] Activate **WebDeploy** and note credentials from the `.publishSettings` profile
- [ ] Prefer a **paid plan** if you expect large Excel imports (free tier app pool idle timeout can interrupt jobs)

### Environment variables

Websites → Manage website → Scripting → **Environment Variables**:

- [ ] `ConnectionStrings__DefaultConnection` — MonsterASP MSSQL connection string
- [ ] `ASPNETCORE_ENVIRONMENT` — `Production`
- [ ] `AdminSeed__Email` — unique admin email (not `admin@tansekak.local`)
- [ ] `AdminSeed__Password` — strong password (not `Admin@12345`)
- [ ] `R2__AccountId` — Cloudflare account ID
- [ ] `R2__AccessKeyId` — R2 S3 API access key
- [ ] `R2__SecretAccessKey` — R2 S3 API secret
- [ ] `R2__BucketName` — e.g. `tansekak-imports`

Do **not** set `Frontend__Origin` — SPA and API are same-origin on MonsterASP.

---

## 2. Cloudflare R2 (large imports > 20 MB)

- [ ] [Cloudflare Dashboard](https://dash.cloudflare.com) → **R2 Object Storage** → Create bucket `tansekak-imports`
- [ ] Copy **Account ID** → `R2__AccountId`
- [ ] **Manage R2 API Tokens** → Create token with **Object Read & Write** scoped to the bucket
- [ ] Save Access Key ID → `R2__AccessKeyId` and Secret Key → `R2__SecretAccessKey`

### Bucket CORS (required for browser PUT uploads)

R2 → bucket → Settings → **CORS policy**:

```json
[
  {
    "AllowedOrigins": ["https://<your-monsterasp-domain>"],
    "AllowedMethods": ["PUT"],
    "AllowedHeaders": ["Content-Type"],
    "ExposeHeaders": ["ETag"],
    "MaxAgeSeconds": 3600
  }
]
```

- [ ] Replace `<your-monsterasp-domain>` with your live MonsterASP URL (exact match, including `https://`)
- [ ] Optional: lifecycle rule to delete objects under `imports/` after 1 day

---

## 3. GitHub Actions deploy secrets

GitHub repo → Settings → Secrets and variables → Actions:

- [ ] `WEBSITE_NAME` — from MonsterASP WebDeploy profile
- [ ] `SERVER_COMPUTER_NAME` — from MonsterASP WebDeploy profile
- [ ] `SERVER_USERNAME` — from MonsterASP WebDeploy profile
- [ ] `SERVER_PASSWORD` — from MonsterASP WebDeploy profile

CI deploys on push to `main` (skipped on pull requests). You can also trigger manually via **Actions → CI & Deploy → Run workflow**.

---

## 4. Deploy

- [ ] Confirm all MonsterASP env vars and GitHub secrets are set
- [ ] Push to `main` or run the workflow manually
- [ ] Watch GitHub Actions for build/test/publish/deploy success
- [ ] On first boot: EF migrations run, reference data seeds, admin user is created

---

## 5. Post-deploy verification

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
| App fails to start | Missing or invalid env vars — check MonsterASP application logs |
| `localhost` connection error | `ConnectionStrings__DefaultConnection` not set or still pointing locally |
| Admin login fails | Wrong `AdminSeed__*` values; user already created on first boot with different password |
| Large import returns 503 | R2 env vars missing or incomplete |
| Large import CORS error | R2 CORS `AllowedOrigins` does not exactly match your MonsterASP domain |
| Import job interrupted | Free-tier app pool timeout — upgrade plan or retry |
