# Tansekak (تنسيقك)

Egyptian Thanaweya Amma admission eligibility checker based on previous year official cutoffs.

## Stack

- **Backend:** ASP.NET Core 10, EF Core, SQL Server LocalDB, Identity
- **Frontend:** Angular 19, RTL Arabic UI
- **Architecture:** Clean Architecture (`Domain`, `Application`, `Infrastructure`, `Api`)

## Prerequisites

- .NET 10 SDK
- Node.js 20+
- SQL Server LocalDB (included with Visual Studio / SQL Express)

## Quick Start

### 1. API

```powershell
cd src/Tansekak.Api
dotnet run
```

API runs at `http://localhost:5080` and auto-migrates + seeds from `SeededData/` on first startup.

**Default admin:**
- Email: `admin@tansekak.local`
- Password: `Admin@12345`

Admin login is at `/admin/login` (not linked from the public site).

### 2. Frontend

```powershell
cd client
npm install
npm start
```

App runs at `http://localhost:4200` with API proxy.

## Public Features

- Select track (Science / Mathematics / Literature) with inline Arabic validation
- Enter total score
- View eligible results only (`score >= cutoff`), sorted by closest match
- Search loaded results by university or faculty name
- Load More pagination (20 per page) — shows all eligible colleges via "تحميل المزيد" or "عرض كل الكليات"

## Admin Features

Two pages after login at `/admin/login`:

- **حدود القبول** (`/admin`) — manage cutoffs with university/faculty dropdowns (existing pairs only)
- **استيراد** (`/admin/import`) — import cutoffs from `.md` Markdown files for a single selected track per upload (replaces existing cutoffs for that year+track)

## Tests

```powershell
dotnet test
```

## Database

Connection string in `src/Tansekak.Api/appsettings.json`:

```
Server=(localdb)\mssqllocaldb;Database=TansekakDev2;Trusted_Connection=True;TrustServerCertificate=True
```

Seed data lives in [`SeededData/`](SeededData/) and is loaded once on first startup (catalog + cutoffs from JSON).

## UX Simplification (2026)

- Prediction results show **eligible colleges only** (no Near / Not Available categories)
- Public navigation hides admin access; admins use `/admin/login` directly
- Admin UI consolidated to cutoffs CRUD + single-track import
- Immediate Arabic inline validation on public home, cutoffs, and import forms
