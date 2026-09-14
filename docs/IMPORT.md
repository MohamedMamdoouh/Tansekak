# Import guide

Operator reference for the current-stage Tansekak admin imports. Both flows target the **current published admission year** only. There is no preview step: the uploaded file is validated, then either applied in full or rejected in full.

Administrator sign-in is required. Public prediction has no cutoffs until Science and Literature Markdown have been imported for the current year.

---

## Cutoff Markdown

### Admin UI

Path: **`/admin/import`**.

The page loads the **current published year** (year number and maximum score). There is **no year picker**.

Two file slots:

| Slot | Track | Typical file |
| --- | --- | --- |
| ملف الشعبة العلمية | `Science` | `science-2026.md` |
| ملف الشعبة الأدبية | `Literature` | `literature-2026.md` |

You may upload **one slot or both**. Each selected file is posted separately and **replaces that track for the current year**. The other track is left unchanged.

Accepts `.md` only. During upload, a full-page overlay blocks leaving or refreshing the page.

### Replace semantics

| Uploaded track | Existing rows deleted for that year | Rows inserted as |
| --- | --- | --- |
| **Science** | `Science` **and** legacy `Mathematics` | `Science` |
| **Literature** | `Literature` only | `Literature` |

Science and Mathematics share one bucket. Importing Science clears both so leftover Mathematics cutoffs cannot sit beside the new Science list.

### Sample files and first-run

Repo samples:

- `SeededData/cutoffs/science-2026.md`
- `SeededData/cutoffs/literature-2026.md`

The `2026` in the filename is the **official source cycle**, not the admission year stored in the database. These files are **not auto-seeded** and are **not copied into the Docker image**. Only `SeededData/*.json` is published into the API as `SeedData/` (governorates, universities, faculties, university–faculty links).

On a production host you cannot read the Markdown from the container. Clone the git repo locally, take the files from `SeededData/cutoffs/`, and upload them in the browser.

Bootstrap creates admission year **2027** (max score 320) and marks it current. **Importing the 2026 lists into that current year is the intended first-run path**, unless an administrator publishes a different year on `/admin/years`.

### File format

Markdown **pipe table**. Parser rules:

- Lines that do **not** start with `|` are ignored (comments and titles above the table are fine).
- A header row containing `الكلية` or `الحد` is skipped.
- A separator row containing `---` is skipped.
- Data rows: first cell = college label, second cell = cutoff score.
- Score is parsed with **invariant culture** (`309.5`, not a locale decimal comma as the only separator).

Example (fake rows):

```markdown
# Optional comment — this line is ignored

| الكلية | الحد الأدنى |
| --- | --- |
| طب القاهرة | 308.0 |
| هندسة عين شمس | 301.5 |
| آداب الإسكندرية | 244.0 |
```

### Matching and validation

College labels are matched to the **university–faculty catalog** after Arabic normalization (aliases such as faculty + university short name). The resolved faculty must **allow the imported track**.

Each score must be **greater than 0** and **less than or equal to** the year’s `MaximumScore`.

Any unresolved college name, disallowed track, non-positive or over-max score, duplicate college, or parse error **fails the entire import**. Existing cutoffs for that track are not changed.

### API

```http
POST /api/admin/admission-years/{yearId}/import
Content-Type: multipart/form-data
Authorization: Administrator cookie

file   = <markdown file>
track  = Science | Literature
```

- `.md` only (`ONLY_MD_FILES` otherwise).
- Request size limit **10 MB**.
- `{yearId}` is the current year’s id when using the admin UI.

Success inserts the resolved rows after the track-bucket delete described above.

---

## Student results Excel

### Admin UI

Path: **`/admin/import-results`**.

Targets the **current published year** (no year picker). The import **replaces all student results for that year**.

Accepts `.xlsx` only. Do **not** commit `.xlsx` workbooks to the repo (they are listed in `.gitignore`).

### Required columns

Headers are matched by **English name**, not column position. Names are compared case-insensitively after trim and treating spaces as underscores.

| Header | Content |
| --- | --- |
| `seating_no` | Seating number |
| `arabic_name` | Student name |
| `total_degree` | Total score |
| `student_case_desc` | Case / track description |

Extra columns are ignored. Empty rows are skipped. Duplicate `seating_no` values fail the import.

**Totals are not capped** at the year’s maximum score. Negative totals and non-numeric totals fail validation.

### Track inference

Track is inferred from `student_case_desc` and/or the seating number. Stored values are canonical **`Science`** or **`Literature`** (legacy Mathematics is treated as Science).

- Case text such as علمي / علمي علوم / علمي رياضة → Science; أدبي → Literature.
- Standard 7-digit seating numbers starting with `2`: second digit `0–3` → Literature, `4–9` → Science. Used when case text does not yield a track.

### Upload paths

Limit used by the UI and API: **20 MB** (`20 × 1024 × 1024` bytes).

**Direct (file ≤ 20 MB)** — synchronous:

```http
POST /api/admin/admission-years/{yearId}/import-results
Content-Type: multipart/form-data

file = <xlsx>
```

Posting a larger file on this endpoint returns **413** with `FILE_TOO_LARGE`.

**Large (file > 20 MB)** — Cloudflare R2, then a background job:

1. `POST /api/admin/admission-years/{yearId}/import-results/upload-url` with `{ "fileName": "…" }` → presigned URL + `objectKey`.
2. **PUT** the workbook to the presigned R2 URL.
3. `POST /api/admin/admission-years/{yearId}/import-results/from-storage` with `{ "objectKey": "…" }` → `{ "jobId": "…" }`.
4. Poll `GET /api/admin/import-jobs/{jobId}` every **~2.5 seconds** until `status` is `completed` or `failed`.

If R2 is not configured, the large path returns **503** with `R2_NOT_CONFIGURED`. Direct uploads ≤ 20 MB still work without R2.

After the job finishes, the object is deleted from R2.

### Overlay

While a student-results (or cutoff) upload is in progress, a modal overlay blocks leaving the page. Browser refresh is intercepted; in-app navigation asks for confirmation. Cancel aborts the in-flight request.

---

## Validation

- There is **no preview-only** import. Submit validates, then commits or rejects.
- **Any validation error rejects the entire file.** Nothing is partially applied.
- Failed responses include per-row errors with:

| Field | Meaning |
| --- | --- |
| `rowNumber` | Spreadsheet row or Markdown line number (`0` for file-level errors) |
| `column` | Field name (`الكلية`, `الحد الأدنى`, `seating_no`, …) |
| `errorCode` | Stable code (for example `UNRESOLVED_COLLEGE`, `MISSING_COLUMN`) |
| `message` | Arabic operator message |

The admin UI lists those row errors under the import result. Fix the file and upload again.
