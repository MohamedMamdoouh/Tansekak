# Production re-import after 3-track revert

If production already ran migration `20260914220000_CollapseMathematicsIntoScience`, code deploy alone is not enough. Re-sync data using the steps below.

## 1. Deploy the 3-track code

Deploy the backend and frontend build that restores distinct `Science`, `Mathematics`, and `Literature` handling.

## 2. Restore faculty allowed tracks

Re-seed or update `Faculties.AllowedTracks` from [`SeededData/Faculties.json`](../SeededData/Faculties.json) so Mathematics-only faculties (e.g. هندسة) and dual-track faculties are correct again.

Options:

- Re-run catalog seed if your deployment supports it, or
- Update faculties via the admin API / direct SQL using the seed JSON as source of truth.

## 3. Re-import cutoff markdown (current year)

At `/admin/import`, import **three separate files** for the active admission year:

| Track | Seed file | Arabic label |
| --- | --- | --- |
| `Science` | `SeededData/cutoffs/science-2026.md` | علمي علوم |
| `Mathematics` | `SeededData/cutoffs/mathematics-2026.md` | علمي رياضة |
| `Literature` | `SeededData/cutoffs/literature-2026.md` | أدبي |

Each import replaces cutoffs for **that track only** for the selected year.

## 4. Re-import student results Excel

At `/admin/import-results`, upload the Thanaweya Excel for the current year again. This replaces all student rows and re-resolves `Track` from `studentCaseDesc` and seating number so علمي رياضة students are stored as `Mathematics` again.

## 5. Verify

- `GET /api/config` → `tracks: ["Science","Mathematics","Literature"]`
- Predict with the same score as `Science` vs `Mathematics` → different faculty lists (e.g. طب vs هندسة)
- Thanaweya lookup: seating `24xxxxx` → Mathematics rank cohort; `27xxxxx` → Science cohort
- Admin cutoffs list shows three track values with Arabic labels
