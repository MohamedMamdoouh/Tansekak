# Tansekak API reference

JSON is camelCase. Unless noted, every controller response is wrapped in the envelope below. `GET /health` is the exception.

## Envelope

```json
{
  "success": true,
  "message": "Operation completed successfully.",
  "errorCode": null,
  "data": {},
  "errors": null
}
```

| Field | Type | Notes |
| --- | --- | --- |
| `success` | boolean | `true` on success, `false` on failure |
| `message` | string | Success default is English `"Operation completed successfully."` Some success paths override it (logout, cutoff delete, imports). Failures use **Arabic** text from `ArabicErrorCatalog` (or a formatted/custom Arabic string) |
| `errorCode` | string? | Present on failure. Stable English code from `ApiErrorCodes` |
| `data` | object? | Payload on success; typically `null` on failure |
| `errors` | array? | Optional field- or row-level details |

`errors[]` shape:

| Field | Type | Notes |
| --- | --- | --- |
| `field` | string | Model / column name |
| `message` | string | Arabic message |
| `rowNumber` | number? | Import row, when applicable |
| `errorCode` | string? | Per-row import code, when applicable |

`GET /health` is **not** wrapped. It returns `{ "status": "healthy" }`.

## Auth

Admin APIs use **cookie ASP.NET Core Identity** (not JWT). Send cookies with the request (`credentials: include`). Login sets a persistent cookie.

| Rule | Behavior |
| --- | --- |
| Role | Admin routes require role `Administrator` |
| Unauthenticated admin call | `401` `NOT_AUTHENTICATED` — Arabic: يجب تسجيل الدخول للمتابعة. |
| Authenticated but not Administrator | `403` `FORBIDDEN` — Arabic: ليس لديك صلاحية لتنفيذ هذا الإجراء. |
| Login | Public. Wrong email/password, lockout, or a non-Administrator account → `401` `INVALID_CREDENTIALS` (non-Administrator is signed out immediately) |
| `POST /api/admin/auth/logout` and `GET /api/admin/auth/me` | Require an authenticated cookie (`[Authorize]`). `/me` still returns `401` `NOT_AUTHENTICATED` if the user is not an Administrator |

## Public endpoints

### `GET /api/config`

Returns app settings for the **published current** admission year.

**`data`**

| Field | Type | Notes |
| --- | --- | --- |
| `appName` | string | From configuration (`Tansekak:AppName`) |
| `currentYear` | number | Current year number (not the year entity id) |
| `maximumScore` | number | Max score for that year |
| `tracks` | string[] | Always `["Science","Literature"]` |

No current year → `503` `NO_CURRENT_YEAR`.

### `POST /api/admission/predict`

Predict faculties the student can reach in the **current** year.

**Body**

| Field | Type | Notes |
| --- | --- | --- |
| `track` | string | Required. Canonical values: `Science`, `Literature`. Aliases such as `Mathematics`, `علمي`, `أدبي` are accepted and canonicalized (`Mathematics` → `Science`) |
| `score` | number | Required. Must be ≥ 0. Must not exceed the current year’s `maximumScore` |
| `page` | number | Optional. Default `1` |
| `pageSize` | number | Optional. Default `10`. Max `100` (`PaginationConstants`) |

**`data`**

```json
{
  "results": [
    {
      "university": { "nameAr": "جامعة القاهرة" },
      "faculty": { "nameAr": "طب" }
    }
  ],
  "hasMore": true,
  "totalCount": 142
}
```

Eligibility and ordering:

1. Current published year only
2. Cutoff track is in the requested **track bucket** (`Science` includes stored `Mathematics` rows; `Literature` is its own bucket)
3. `score >= cutoffScore`
4. Faculty `AllowedTracks` includes the requested canonical track
5. One row per university–faculty (Science preferred over leftover Mathematics, then lowest cutoff id)
6. Sorted **closest first**: `abs(score − cutoffScore)` ascending

No current year → `503` `NO_CURRENT_YEAR`. Score above max → `400` `SCORE_EXCEEDS_MAX` (Arabic message includes the max as `{0}`). Invalid track / paging → `400` `VALIDATION_FAILED`.

### `GET /api/thanaweya-results/{seatingNo}`

Lookup by seating number for the **current year only**.

**`data`**

| Field | Type | Notes |
| --- | --- | --- |
| `seatingNo` | string | |
| `arabicName` | string | |
| `totalDegree` | number | |
| `studentCaseDesc` | string | |
| `year` | number | Current year number |
| `track` | string? | `Science` or `Literature` when resolvable |
| `trackRank` | number? | Rank among the same track in the current year |
| `trackTotalStudents` | number? | Count of students in that track in the current year |

Missing result → `404` `STUDENT_RESULT_NOT_FOUND`. No current year → `503` `NO_CURRENT_YEAR`. Empty seating number is treated as not found.

Track is resolved from stored track, then `studentCaseDesc`, then seating number. Rank is among peers in the same canonical track. **Ties** (same `totalDegree`) are broken by **lower seating number** (ordinal string compare) ranking higher.

### `GET /health`

Unwrapped. `{ "status": "healthy" }`.

## Admin endpoints

All routes below except `POST /api/admin/auth/login` require an Identity cookie. Dashboard, catalog, years, cutoffs, and import routes also require `Administrator`.

There is **no delete** on governorates, universities, faculties, or university–faculties. Cutoffs and admission years do support `DELETE`.

| Method | Path | Body / query | `data` notes |
| --- | --- | --- | --- |
| `POST` | `/api/admin/auth/login` | `{ email, password }` | `{ email, role }` with `role` = `"Administrator"` |
| `POST` | `/api/admin/auth/logout` | — | Empty object. Message: `"Logged out successfully."` |
| `GET` | `/api/admin/auth/me` | — | `{ email, role }` |
| `GET` | `/api/admin/dashboard` | — | Counts: `governoratesCount`, `universitiesCount`, `facultiesCount`, `universityFacultiesCount`, **`cutoffsCount`** (API returns it even though the admin UI hides it), `studentResultsCount`, `currentYear` (`null` if none published) |
| `GET` | `/api/admin/governorates` | — | `[{ id, nameAr }]` |
| `GET` | `/api/admin/governorates/{id}` | — | `{ id, nameAr }` |
| `POST` | `/api/admin/governorates` | `{ nameAr }` | Created governorate |
| `PUT` | `/api/admin/governorates/{id}` | `{ nameAr }` | Updated governorate. Missing id → `404` `NOT_FOUND` |
| `GET` | `/api/admin/universities` | `search`, `governorateId`, `type` | `[{ id, nameAr, governorateId, type, governorateName? }]`. `type` is `"Public"` or `"Institute"` |
| `GET` | `/api/admin/universities/{id}` | — | Single university |
| `POST` | `/api/admin/universities` | `{ nameAr, governorateId, type }` | Created university |
| `PUT` | `/api/admin/universities/{id}` | `{ nameAr, governorateId, type }` | Updated university |
| `GET` | `/api/admin/faculties` | `search` | `[{ id, nameAr, allowedTracks }]` where `allowedTracks` is `Science` / `Literature` |
| `GET` | `/api/admin/faculties/{id}` | — | Single faculty |
| `POST` | `/api/admin/faculties` | `{ nameAr, allowedTracks }` | At least one allowed track required |
| `PUT` | `/api/admin/faculties/{id}` | `{ nameAr, allowedTracks }` | Updated faculty |
| `GET` | `/api/admin/university-faculties` | `search`, `universityId`, `facultyId` | `[{ id, universityId, facultyId, universityName?, facultyName? }]` |
| `GET` | `/api/admin/university-faculties/{id}` | — | Single link |
| `POST` | `/api/admin/university-faculties` | `{ universityId, facultyId }` | Created link |
| `PUT` | `/api/admin/university-faculties/{id}` | `{ universityId, facultyId }` | Updated link |
| `GET` | `/api/admin/admission-years` | — | `[{ id, year, maximumScore, isCurrent }]` newest year first |
| `GET` | `/api/admin/admission-years/current` | — | Current year, or `404` `NOT_FOUND` if none published |
| `GET` | `/api/admin/admission-years/{id}` | — | Single year |
| `POST` | `/api/admin/admission-years` | `{ year, maximumScore }` | Created with `isCurrent: true`. **Only one year may exist** — if any year is already stored → `400` `ADMISSION_YEAR_LIMIT_REACHED`. Year must be 2000–2100; `maximumScore` > 0 and ≤ 1000 |
| `PUT` | `/api/admin/admission-years/{id}` | `{ year, maximumScore }` | Updates year number and max score. Does not change `isCurrent` |
| `DELETE` | `/api/admin/admission-years/{id}` | — | Deletes the year and cascades cutoffs, student results, and import jobs (best-effort R2 cleanup). Missing id → `404` `NOT_FOUND`. To add a different year, delete the current one first |
| `GET` | `/api/admin/admission-cutoffs` | `yearId`, `search`, `track`, `page`, `pageSize` | Paged `{ items, totalCount, page, pageSize }`. `page` default `1`, `pageSize` default `10`, max `100`. `search` matches university or faculty Arabic name. `track` uses the same bucket as predict |
| `GET` | `/api/admin/admission-cutoffs/{id}` | — | `{ id, admissionYearId, universityFacultyId, track, cutoffScore, universityName?, facultyName? }` |
| `POST` | `/api/admin/admission-cutoffs` | `{ admissionYearId, universityFacultyId, track, cutoffScore }` | Duplicate year+faculty+track bucket → `CUTOFF_DUPLICATE`. Faculty must allow the track |
| `PUT` | `/api/admin/admission-cutoffs/{id}` | `{ admissionYearId, universityFacultyId, track, cutoffScore }` | Same rules as create |
| `DELETE` | `/api/admin/admission-cutoffs/{id}` | — | Empty object. Message: `"Deleted successfully."` |
| `POST` | `/api/admin/admission-years/{yearId}/import` | multipart: `file` (`.md`), `track` | Cutoff markdown import, max **10 MB**. Empty file → `FILE_REQUIRED`. Missing track → `TRACK_REQUIRED`. Non-`.md` → `ONLY_MD_FILES` |
| `POST` | `/api/admin/admission-years/{yearId}/import-results` | multipart: `file` (`.xlsx`) | Direct student-result import, max **20 MB**. Over that → `413` `FILE_TOO_LARGE`. Non-`.xlsx` → `ONLY_XLSX_FILES` |
| `POST` | `/api/admin/admission-years/{yearId}/import-results/from-upload` | multipart: `file` (`.xlsx`) | Same-origin staged upload for files **> 20 MB** and **≤ 100 MB**. Streams the file to R2 then starts an async job. `data`: `{ jobId }`. Message: `"Import started."`. Over 100 MB → `413` `FILE_TOO_LARGE`. R2 missing → `503` `R2_NOT_CONFIGURED`. Non-`.xlsx` → `ONLY_XLSX_FILES` |
| `POST` | `/api/admin/admission-years/{yearId}/import-results/upload-url` | `{ fileName }` | `{ uploadUrl, objectKey }` for a 15-minute R2 PUT (API compatibility; the admin UI uses `from-upload`). R2 missing → `503` `R2_NOT_CONFIGURED`. Empty name → `FILE_NAME_REQUIRED`. Must be `.xlsx` |
| `POST` | `/api/admin/admission-years/{yearId}/import-results/from-storage` | `{ objectKey }` | Starts an async job. `data`: `{ jobId }`. Message: `"Import started."`. `objectKey` must start with `imports/{yearId}/` |
| `GET` | `/api/admin/import-jobs/{id}` | `id` is a GUID | `{ id, status, importedCount, message, createdAtUtc, completedAtUtc }`. `status`: `queued` \| `running` \| `completed` \| `failed` |

Successful import envelopes put `ImportResultDto` in `data`: `{ success, message, importedCount?, errors? }`. Failed imports return `400` `VALIDATION_FAILED` with row `errors`.

## Status codes

| HTTP | When |
| --- | --- |
| `400` | Validation / business-rule failures (`VALIDATION_FAILED`, FluentValidation, `INVALID_TRACK`, cutoff/year duplicates, import parse errors, and similar `ApiErrorCodes`) |
| `401` | Not signed in (`NOT_AUTHENTICATED`) or failed login (`INVALID_CREDENTIALS`) |
| `403` | Signed in but missing the `Administrator` role (`FORBIDDEN`) |
| `404` | Missing entity (`NOT_FOUND`, `STUDENT_RESULT_NOT_FOUND`, `ADMISSION_YEAR_NOT_FOUND`, `UNIVERSITY_FACULTY_NOT_FOUND`) |
| `413` | Direct student-result upload over 20 MB, or staged `from-upload` over 100 MB (`FILE_TOO_LARGE`) |
| `500` | Unexpected exception (`INTERNAL_ERROR`) |
| `503` | R2 not configured (`R2_NOT_CONFIGURED`) or no published current year (`NO_CURRENT_YEAR`) |

## Error codes

Fail `message` is the Arabic catalog string unless the server formats a placeholder or substitutes a custom Arabic string.

Codes with `{0}`: `SCORE_EXCEEDS_MAX` (maximum score), `MISSING_COLUMN` (column name), `UNRESOLVED_COLLEGE` (college text from the file).

| Code | Arabic message |
| --- | --- |
| `INTERNAL_ERROR` | حدث خطأ غير متوقع. حاول مرة أخرى لاحقاً. |
| `NOT_FOUND` | العنصر المطلوب غير موجود. |
| `VALIDATION_FAILED` | يرجى التحقق من البيانات المدخلة. |
| `INVALID_CREDENTIALS` | بيانات الدخول غير صحيحة. |
| `NOT_AUTHENTICATED` | يجب تسجيل الدخول للمتابعة. |
| `FORBIDDEN` | ليس لديك صلاحية لتنفيذ هذا الإجراء. |
| `SERVICE_UNAVAILABLE` | الخدمة غير متاحة حالياً. حاول مرة أخرى لاحقاً. |
| `NO_CURRENT_YEAR` | لم يتم تعيين سنة قبول حالية. |
| `INVALID_TRACK` | الشعبة غير صحيحة. |
| `INVALID_UNIVERSITY_TYPE` | نوع الجامعة غير صحيح. |
| `SCORE_EXCEEDS_MAX` | المجموع يتجاوز الحد الأقصى المسموح ({0}). |
| `CUTOFF_NEGATIVE` | يجب أن يكون الحد الأدنى صفراً أو أكثر. |
| `CUTOFF_DUPLICATE` | يوجد بالفعل حد قبول لهذه الكلية والشعبة في السنة المحددة. |
| `ADMISSION_YEAR_NOT_FOUND` | سنة القبول غير موجودة. |
| `ADMISSION_YEAR_DUPLICATE` | هذه السنة موجودة بالفعل. |
| `ADMISSION_YEAR_LIMIT_REACHED` | يوجد سنة قبول بالفعل. احذفها أولاً لإضافة سنة جديدة. |
| `UNIVERSITY_FACULTY_NOT_FOUND` | كلية الجامعة غير موجودة. |
| `STUDENT_RESULT_NOT_FOUND` | لم يتم العثور على نتيجة لهذا الرقم. |
| `FILE_REQUIRED` | الملف مطلوب. |
| `TRACK_REQUIRED` | الشعبة مطلوبة. |
| `FILE_NAME_REQUIRED` | اسم الملف مطلوب. |
| `OBJECT_KEY_REQUIRED` | مفتاح الملف مطلوب. |
| `ONLY_MD_FILES` | يُسمح فقط بملفات .md. |
| `ONLY_XLSX_FILES` | يُسمح فقط بملفات .xlsx. |
| `FILE_TOO_LARGE` | حجم الملف يتجاوز حد الرفع المباشر (20 ميجابايت). استخدم رفع R2. |
| `R2_NOT_CONFIGURED` | رفع الملفات الكبيرة غير متاح. يرجى ضبط Cloudflare R2. |
| `INVALID_OBJECT_KEY` | مفتاح الملف غير صالح. |
| `IMPORT_JOB_MISSING_KEY` | مهمة الاستيراد لا تحتوي على مفتاح ملف. |
| `ALLOWED_TRACKS_REQUIRED` | يجب تحديد شعبة واحدة على الأقل. |
| `FACULTY_TRACK_NOT_ALLOWED` | الكلية غير متاحة للشعبة المحددة. |
| `REQUIRED` | هذا الحقل مطلوب. |
| `INVALID` | قيمة غير صالحة. |
| `EMPTY` | الملف فارغ أو لا يحتوي على بيانات. |
| `DUPLICATE` | قيمة مكررة. |
| `MISSING_COLUMN` | عمود مطلوب مفقود ({0}). |
| `INVALID_ROW` | تنسيق الصف غير صحيح. |
| `MALFORMED` | تنسيق البيانات غير صحيح. |
| `TRACK_NOT_ALLOWED` | الكلية غير متاحة للشعبة المحددة. |
| `SCORE_MUST_BE_POSITIVE` | يجب أن يكون الحد الأدنى أكبر من صفر. |
| `DUPLICATE_ROW` | صف مكرر في الملف. |
| `COLLEGE_CONTAINS_SCORE` | عمود الكلية يحتوي على قيمة درجة. |
| `CUTOFF_MUST_BE_NUMBER` | يجب أن يكون الحد الأدنى رقماً. |
| `TOTAL_DEGREE_INVALID` | يجب أن يكون المجموع رقماً صالحاً. |
| `TOTAL_DEGREE_NEGATIVE` | يجب أن يكون المجموع صفراً أو أكثر. |
| `WORKBOOK_EMPTY` | ملف Excel لا يحتوي على أوراق عمل. |
| `WORKSHEET_EMPTY` | ورقة العمل لا تحتوي على بيانات. |
| `FILE_NO_DATA_ROWS` | الملف لا يحتوي على صفوف بيانات. |
| `UNRESOLVED_COLLEGE` | تعذر مطابقة "{0}" مع جامعة/كلية في النظام. |

Unknown codes fall back to the `INTERNAL_ERROR` Arabic message.

## OpenAPI

In **Development** only, the JSON spec is at `/openapi/v1.json`. There is **no Swagger UI**. Production does not map OpenAPI.