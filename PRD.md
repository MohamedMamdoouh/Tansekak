# Tansekak

## Product Requirements Document (PRD)

**Product Name:** Tansekak
**Version:** 1.0
**Status:** Approved
**Language:** English

---

## Amendment (Implementation v1.0)

The following deviations apply to the MVP implementation:

- **University scope:** Includes Public Universities and Institutes (not public-only).
- **University entity:** Adds `Type` enum (`Public = 1`, `Institute = 2`).
- **Removed fields:** `NameEn`, `Slug`, and `IsActive` are not used. Entities store `NameAr` only.
- **Admin operations:** No activate/deactivate. Create and edit only for governorates, universities, faculties, university-faculties, and admission years. Only admission cutoffs may be deleted.
- **Import matching:** University and faculty matched by `NameAr` only.
- **Pagination:** First request returns 10 results; Load More adds 10 more (20 max). Default `pageSize = 10`.

---

# 1. Project Overview

## 1.1 Purpose

**Tansekak** is a web application that helps Egyptian Thanaweya Amma students predict which public universities and faculties they are eligible for based on their total score and the official 2026 admission cutoffs.

The system compares the student's score against the current year's (2026) official admission data.

The application is designed to be simple, fast, accurate, and updated every year after the official admission results are announced.

## 1.2 Product Identity

| Field           | Value    |
| --------------- | -------- |
| Product Name    | Tansekak |
| Arabic Name     | تنسيقك   |
| Public Branding | Tansekak |

The product name **Tansekak** must appear in:

- Browser tab title
- Home page header
- Administration panel header
- API configuration response

---

# 2. Project Goals

The system must allow students to:

- Select their academic track.
- Enter their total score.
- View all eligible faculties based on the previous year's admission cutoffs.
- View faculties that are very close to their score.
- View faculties that are above their score.
- Sort results from closest cutoff to farthest cutoff.
- Search within the results.
- Load additional results without reloading the page.

The system must also allow administrators to:

- Manage admission years.
- Import official admission data using Excel or CSV.
- Validate imported files before saving.
- View validation errors.
- Edit imported records manually.
- Publish one admission year as the current year.

---

# 3. Project Scope

## Included

### Public Website

- Home page
- Admission prediction
- Faculty search
- Arabic-only user interface
- Responsive design
- Mobile support

### Administration Panel

- Arabic-only user interface
- Login
- Dashboard
- Admission year management
- University management
- Faculty management
- University–faculty management
- Admission cutoff management
- Excel import
- CSV import
- Import validation
- Manual editing

---

## Out of Scope

The first version MUST NOT include:

- Student accounts
- Student login
- Student profiles
- Favorites
- Notifications
- University ranking
- Faculty descriptions
- Tuition fees
- Images
- Maps
- AI recommendations
- Historical statistics
- Comparison between years
- Private universities
- National universities
- Technological universities

Those features may be implemented in future versions.

---

# 4. Target Users

## Student

A student who wants to know which faculties were available based on the previous year's admission cutoffs.

The student does not need an account.

The student only provides:

- Academic Track
- Total Score

---

## Administrator

An authenticated administrator responsible for maintaining admission data.

The administrator can:

- Login
- Import data
- Edit data
- Activate and deactivate data
- Publish admission years

Authentication will be implemented using ASP.NET Core Identity.

---

# 5. Supported Academic Tracks

The application supports only Egyptian Thanaweya Amma.

Supported tracks:

- Science
- Mathematics
- Literature

No additional education systems are included in Version 1.

---

# 6. Supported Universities

Version 1 supports only:

- Egyptian Public Universities

The database design must remain extensible to support:

- Private Universities
- National Universities
- Technological Universities

without database redesign.

---

# 7. Admission Logic

The prediction is based ONLY on:

- Student Score
- Student Academic Track
- Previous Year's Official Admission Cutoff

The application never predicts future admission results.

It only compares:

Student Score

against

Previous Year's Cutoff Score.

---

# 8. Result Categories

Every admission record belongs to exactly one category.

## Available

Condition

StudentScore >= CutoffScore

---

## Near

Condition

StudentScore < CutoffScore

AND

(CutoffScore - StudentScore) < 1

The threshold value MUST be configurable.

Default value:

0.99

---

## Not Available

Condition

StudentScore < CutoffScore

AND

(CutoffScore - StudentScore) >= NearThreshold

---

# 9. Result Sorting

Results inside each category MUST be sorted by:

ABS(StudentScore - CutoffScore)

Ascending.

The closest cutoff always appears first.

Example

Student Score = 286

Results

286.0

286.25

286.5

287

288

...

---

# 10. Pagination

The system MUST initially return only:

10 results

If additional results exist, show only another 10 results. Total result shoudn't exceed 20.

---

# 11. Search

After results are displayed,

students can search by:

- Faculty Name (NameAr)
- University Name (NameAr)

Search is client-side for the currently loaded results.

---

# 12. Database Design

## Overview

The system is built around six core business entities:

- Governorates
- Universities
- Faculties
- University Faculties
- Admission Years
- Admission Cutoffs

Academic Track is implemented as an enumeration and is **not** stored as a database table.

All entities with a display name store both Arabic and English names (`NameAr`, `NameEn`) in the database and API. The frontend is entirely Arabic and displays only `NameAr` for entity names.

---

# 13. Entity Definitions

## 13.1 Governorate

Represents an Egyptian governorate.

### Properties

| Property | Type          | Required | Notes         |
| -------- | ------------- | -------- | ------------- |
| Id       | int           | Yes      | Primary Key   |
| NameAr   | nvarchar(200) | Yes      | Unique        |
| NameEn   | nvarchar(200) | Yes      | Unique        |
| IsActive | bit           | Yes      | Default: true |

Example

{
"id": 1,
"nameAr": "القاهرة",
"nameEn": "Cairo"
}

### Constraints

- NameAr must be unique.
- NameEn must be unique.
- Governorates cannot be deactivated if referenced by an active university.

---

## 13.2 University

Represents a public Egyptian university.

### Properties

| Property      | Type          | Required | Notes         |
| ------------- | ------------- | -------- | ------------- |
| Id            | int           | Yes      | Primary Key   |
| NameAr        | nvarchar(200) | Yes      | Unique        |
| NameEn        | nvarchar(200) | Yes      | Unique        |
| Slug          | nvarchar(200) | Yes      | Unique        |
| GovernorateId | int           | Yes      | Foreign Key   |
| IsActive      | bit           | Yes      | Default: true |

Example

{
"id": 1,
"nameAr": "جامعة القاهرة",
"nameEn": "Cairo University"
}

### Relationships

- One Governorate
- Many University Faculties

### Constraints

Unique:

- NameAr
- NameEn
- Slug

Delete behavior:

Restrict

---

## 13.3 Faculty

Represents a normalized faculty name in Arabic and English.

Examples:

- Medicine / طب
- Dentistry / أسنان
- Pharmacy / صيدلة
- Engineering / هندسة
- Computer Science / حاسبات ومعلومات
- Commerce / تجارة
- Law / حقوق

### Properties

| Property | Type          | Required | Notes         |
| -------- | ------------- | -------- | ------------- |
| Id       | int           | Yes      | Primary Key   |
| NameAr   | nvarchar(200) | Yes      | Unique        |
| NameEn   | nvarchar(200) | Yes      | Unique        |
| Slug     | nvarchar(200) | Yes      | Unique        |
| IsActive | bit           | Yes      | Default: true |

Example

{
"id": 1,
"nameAr": "حاسبات ومعلومات",
"nameEn": "Computer Science"
}

### Relationships

- Many University Faculties

### Constraints

Unique:

- NameAr
- NameEn
- Slug

Delete behavior:

Restrict

---

## 13.4 UniversityFaculty

Represents the existence of a faculty within a specific university.

Example:

Cairo University / جامعة القاهرة → Computer Science / حاسبات ومعلومات

### Properties

| Property     | Type | Required | Notes         |
| ------------ | ---- | -------- | ------------- |
| Id           | int  | Yes      | Primary Key   |
| UniversityId | int  | Yes      | FK            |
| FacultyId    | int  | Yes      | FK            |
| IsActive     | bit  | Yes      | Default: true |

### Relationships

- One University
- One Faculty
- Many Admission Cutoffs

### Constraints

Unique:

(UniversityId, FacultyId)

Delete behavior:

Restrict

---

## 13.5 AdmissionYear

Represents one admission season.

### Properties

| Property     | Type         | Required | Notes                       |
| ------------ | ------------ | -------- | --------------------------- |
| Id           | int          | Yes      | Primary Key                 |
| Year         | int          | Yes      | Unique                      |
| MaximumScore | decimal(6,2) | Yes      | Example: 320                |
| IsCurrent    | bit          | Yes      | Only one row may be current |
| IsActive     | bit          | Yes      | Default: true               |

### Constraints

- Year must be unique.
- Only one record may have IsCurrent = true.
- MaximumScore must be greater than zero.

Delete behavior:

Restrict

---

## 13.6 AdmissionCutoff

Represents one official admission cutoff.

Example:

2026

Cairo University / جامعة القاهرة

Computer Science / حاسبات ومعلومات

Mathematics

286.5

### Properties

| Property            | Type         | Required | Notes           |
| ------------------- | ------------ | -------- | --------------- |
| Id                  | int          | Yes      | Primary Key     |
| AdmissionYearId     | int          | Yes      | FK              |
| UniversityFacultyId | int          | Yes      | FK              |
| Track               | tinyint      | Yes      | Enum            |
| CutoffScore         | decimal(6,2) | Yes      | Official cutoff |

### Relationships

- One Admission Year
- One University Faculty

### Constraints

Unique:

(AdmissionYearId, UniversityFacultyId, Track)

Validation:

- CutoffScore > 0
- CutoffScore <= AdmissionYear.MaximumScore

Delete behavior:

Restrict

Physical deletion of Admission Cutoff records is allowed.

---

# 14. Enumerations

## AcademicTrack

```text
Science = 1

Mathematics = 2

Literature = 3
```

The enumeration values must never change after release.

---

# 15. Entity Relationships

Governorate (1)
│
│
▼
University (Many)

University (1)
│
│
▼
UniversityFaculty (Many)

Faculty (1)
│
│
▼
UniversityFaculty (Many)

UniversityFaculty (1)
│
│
▼
AdmissionCutoff (Many)

AdmissionYear (1)
│
│
▼
AdmissionCutoff (Many)

---

# 16. Database Rules

The database must enforce the following rules.

## Rule 1

Governorate NameAr must be unique.

Governorate NameEn must be unique.

---

## Rule 2

University NameAr must be unique.

University NameEn must be unique.

---

## Rule 3

Faculty NameAr must be unique.

Faculty NameEn must be unique.

---

## Rule 4

Admission years are unique.

---

## Rule 5

Only one admission year can be current.

---

## Rule 6

A university cannot contain the same faculty more than once.

The combination of UniversityId and FacultyId must be unique.

---

## Rule 7

A cutoff record must be unique for:

Admission Year +
University Faculty +
Track

Duplicate records are forbidden.

---

## Rule 8

Negative scores are not allowed.

---

## Rule 9

Scores greater than the year's MaximumScore are not allowed.

---

## Rule 10

Deactivating referenced records is prohibited.

Physical deletion of referenced records is prohibited.

Cascade delete must never be used.

Admission Cutoff is the only entity that may be physically deleted.

---

# 17. Indexes

The following indexes are required.

Governorate

- IX_Governorate_NameAr (Unique)
- IX_Governorate_NameEn (Unique)

University

- IX_University_NameAr (Unique)
- IX_University_NameEn (Unique)
- IX_University_Slug (Unique)

Faculty

- IX_Faculty_NameAr (Unique)
- IX_Faculty_NameEn (Unique)
- IX_Faculty_Slug (Unique)

UniversityFaculty

- IX_UniversityFaculty_UniversityId
- IX_UniversityFaculty_FacultyId
- UX_UniversityFaculty_UniversityId_FacultyId (Unique)

AdmissionYear

- IX_AdmissionYear_Year (Unique)
- IX_AdmissionYear_IsCurrent

AdmissionCutoff

- IX_AdmissionCutoff_Track
- IX_AdmissionCutoff_UniversityFacultyId

Composite Unique Index

AdmissionYearId

UniversityFacultyId

Track

---

# 18. Seed Data

The project must support seeding the following data.

- Egyptian Governorates (with NameAr and NameEn)
- Egyptian Public Universities (with NameAr and NameEn)
- Standard Faculty List (with NameAr and NameEn)
- University–Faculty Assignments
- One or more Admission Years
- Official Admission Cutoff Records

Every seed record for Governorates, Universities, and Faculties must include both NameAr and NameEn.

The application must be fully functional after seeding.

---

# 19. Business Rules

This section defines the mandatory business rules that the system must enforce.

These rules are part of the business domain and must never be bypassed.

---

# 19.1 Admission Prediction

The prediction engine compares only:

- Student Total Score
- Student Academic Track
- Official Admission Cutoff

No prediction algorithms, AI, machine learning, or statistical estimation are allowed.

The system does not predict future admission results.

It only compares the student's score against the selected admission year's official cutoff data.

---

# 19.2 Admission Year

The system supports multiple admission years.

Exactly one admission year must always be marked as Current.

All public search requests use the Current admission year.

Administrators may switch the Current year at any time.

Changing the Current year immediately affects all public results.

---

# 19.3 Student Input

Students are required to provide only:

- Academic Track
- Total Score

Students are not required to:

- Create an account
- Login
- Provide personal information

---

# 19.4 Academic Track

Supported tracks:

- Science
- Mathematics
- Literature

Any other value must be rejected.

---

# 19.5 Score Validation

Student score:

- Must be greater than or equal to zero.
- Must not exceed the Current Admission Year's MaximumScore.

Example

MaximumScore = 320

Valid

0
145.5
286
320

Invalid

-1
321
500

---

# 19.6 Available Category

A faculty is Available when

StudentScore >= CutoffScore

---

# 19.7 Near Category

A faculty is Near when

StudentScore < CutoffScore

AND

(CutoffScore - StudentScore) < NearThreshold

NearThreshold must be configurable.

Default value:

0.99

---

# 19.8 Not Available Category

A faculty is Not Available when

StudentScore < CutoffScore

AND

(CutoffScore - StudentScore) >= NearThreshold

---

# 19.9 Result Ordering

Results inside every category are sorted by

ABS(StudentScore - CutoffScore)

Ascending.

The closest score always appears first.

Example

Student Score = 286

Available

286
285.5
285

Near

286.25
286.5
286.75

Not Available

287
288
290

---

# 19.10 Search

Students may search results by

- Faculty Name (NameAr)
- University Name (NameAr)

Search is case-insensitive.

Search is performed on the currently loaded results.

---

# 19.11 Bilingual Data Storage

Arabic and English names represent the same entity and must always be maintained together.

Both NameAr and NameEn are mandatory for every Governorate, University, and Faculty.

The API must always return both names.

The frontend is entirely Arabic. All UI text, labels, and messages are in Arabic.

The frontend displays only NameAr for entity names. NameEn is stored and returned by the API but is not shown in the frontend.

No language switching is provided.

No translation logic or hardcoded name mapping is allowed in the frontend.

---

# 19.12 Load More

The first request returns only 10 results, and another 10 in max.

---

# 20. Administration Rules

Only authenticated administrators may access the Administration Panel.

Authentication is implemented using ASP.NET Core Identity.

Anonymous users must never access any administration endpoint.

---

# 20.0 Governorates

Administrators may:

- Create (NameAr and NameEn are required)
- Edit (NameAr and NameEn must be maintained together)
- Activate
- Deactivate

Deactivation is allowed only when the governorate is not referenced by any active university.

Physical deletion is not allowed.

---

# 20.1 Universities

Administrators may:

- Create (NameAr and NameEn are required)
- Edit (NameAr and NameEn must be maintained together)
- Activate
- Deactivate

Deactivation is allowed only when the university is not referenced by any active UniversityFaculty or AdmissionCutoff.

Otherwise the operation must fail.

Physical deletion is not allowed.

---

# 20.2 Faculties

Administrators may:

- Create (NameAr and NameEn are required)
- Edit (NameAr and NameEn must be maintained together)
- Activate
- Deactivate

Deactivation is allowed only when the faculty is not referenced by any active UniversityFaculty or AdmissionCutoff.

Physical deletion is not allowed.

---

# 20.3 University Faculties

Administrators may:

- Create
- Edit
- Activate
- Deactivate
- Search
- Filter

A university cannot be assigned the same faculty more than once.

Deactivation is allowed only when the university faculty is not referenced by any AdmissionCutoff.

Physical deletion is not allowed.

---

# 20.4 Admission Years

Administrators may

Create

Edit

Activate

Deactivate

Publish

Only one admission year may be Current.

Publishing a year automatically sets every other year to IsCurrent = false.

Physical deletion is not allowed.

---

# 20.5 Admission Cutoffs

Administrators may

Create

Edit

Delete

Search

Filter

Duplicate records are forbidden.

Admission Cutoff is the only entity that supports physical deletion.

---

# 21. Excel / CSV Import

The system must support

- Excel (.xlsx)
- CSV (.csv)

No other file types are supported.

---

# 21.1 Import Process

The complete workflow is

Upload

↓

Read File

↓

Validate

↓

Generate Validation Report

↓

If validation succeeds

↓

Import Data

The database must never be modified before validation completes successfully.

---

# 21.2 Column Mapping

Columns are identified by

Column Name

NOT

Column Position.

The following columns are mandatory.

| Column      | Required |
| ----------- | -------- |
| University  | Yes      |
| Faculty     | Yes      |
| Track       | Yes      |
| CutoffScore | Yes      |

Example

Valid

University,Faculty,Track,CutoffScore

Also Valid

Track,CutoffScore,Faculty,University

The order does not matter.

---

# 21.3 Supported Track Values

Science

Mathematics

Literature

Values are case-insensitive.

Any other value is invalid.

---

# 21.4 Validation Rules

Every row must be validated before import.

Validation includes

University exists (matched by NameAr or NameEn)

Faculty exists (matched by NameAr or NameEn)

University offers the specified faculty

Track is valid

Score is numeric

Score > 0

Score <= MaximumScore

No duplicate rows

No duplicate database records

If any validation fails,

the entire import is rejected.

Partial imports are not allowed.

---

# 21.5 Duplicate Detection

Duplicates inside the uploaded file are invalid.

Duplicates against existing database records are invalid.

A duplicate is defined as

AdmissionYear

-

University Faculty

-

Track

Import rows are matched to a University Faculty by resolving the University and Faculty columns to an existing UniversityFaculty record.

---

# 21.6 Error Report

If validation fails,

the user receives a complete validation report.

Each error contains

- Row Number
- Column
- Error Code
- Error Message

Example

Row 25

Column

Faculty

Message

Faculty does not exist.

---

Row 42

Column

University

Message

University does not offer this faculty.

---

# 21.7 Successful Import

When validation succeeds,

all rows are imported inside a single database transaction.

If any database error occurs,

the entire transaction must roll back.

No partial import is allowed.

---

# 22. Manual Editing

Administrators may edit imported records.

The same validation rules apply to manual edits.

Duplicate records must never be created.

---

# 23. Delete and Deactivation Operations

Governorates

Universities

Faculties

University Faculties

Admission Years

must use Activate / Deactivate operations only.

Physical deletion is not allowed for these entities.

Deactivation must respect foreign key constraints and business rules.

Cascade Delete is prohibited.

Admission Cutoff is the only entity that may be physically deleted.

---

# 24. Error Handling

Validation errors return

HTTP 400

Unauthorized requests return

HTTP 401

Forbidden requests return

HTTP 403

Unexpected errors return

HTTP 500

All error responses must use a consistent response model.

Example

{
"success": false,
"message": "...",
"errors": [
...
]
}

---

# 25. Public API Specification

Base URL

/api

All public endpoints are anonymous.

No authentication is required.

---

## 25.1 Get Application Configuration

Endpoint

GET /api/config

Purpose

Returns all information required to initialize the home page.

Response

{
"appName": "tansekak",
"currentYear": 2026,
"maximumScore": 320,
"nearThreshold": 0.99,
"tracks": [
"Science",
"Mathematics",
"Literature"
]
}

---

## 25.2 Predict Admission

Endpoint

POST /api/admission/predict

Request

{
"track": "Science",
"score": 286,
"page": 1,
"pageSize": 20
}

Validation

Track is required.

Track must be valid.

Score must be greater than or equal to zero.

Score must not exceed MaximumScore.

Page must be greater than zero.

PageSize must be greater than zero.

Default PageSize = 20.

---

Response

{
"available": [
...
],
"near": [
...
],
"notAvailable": [
...
],
"hasMore": true
}

---

Admission Result DTO

{
"university": {
"nameAr": "جامعة القاهرة",
"nameEn": "Cairo University"
},
"faculty": {
"nameAr": "حاسبات ومعلومات",
"nameEn": "Computer Science"
},
"track": "Mathematics",
"cutoffScore": 286,
"studentScore": 286.5,
"difference": 0.5,
"status": "Available"
}

Status values

Available

Near

NotAvailable

---

## 25.3 Load More

Additional results are retrieved by increasing

Page

The sorting order must remain identical.

No duplicated records may appear.

---

# 26. Administration API

All administration endpoints require authentication.

Authorization

Authenticated Administrator

---

## Authentication

POST /api/admin/auth/login

POST /api/admin/auth/logout

GET /api/admin/auth/me

Authentication is implemented using ASP.NET Core Identity.

---

## Governorates

GET

POST

PUT

PATCH (Activate / Deactivate)

No Delete endpoint.

Response Example

{
"id": 1,
"nameAr": "القاهرة",
"nameEn": "Cairo"
}

---

## Universities

GET

GET by Id

POST

PUT

PATCH (Activate / Deactivate)

Search

Filtering

No Delete endpoint.

Response Example

{
"id": 1,
"nameAr": "جامعة القاهرة",
"nameEn": "Cairo University",
"slug": "cairo-university",
"governorateId": 1,
"isActive": true
}

---

## Faculties

GET

GET by Id

POST

PUT

PATCH (Activate / Deactivate)

Search

Filtering

No Delete endpoint.

Response Example

{
"id": 15,
"nameAr": "حاسبات ومعلومات",
"nameEn": "Computer Science",
"slug": "computer-science",
"isActive": true
}

---

## University Faculties

GET

GET by Id

POST

PUT

PATCH (Activate / Deactivate)

Search

Filtering

No Delete endpoint.

---

## Admission Years

GET

GET by Id

POST

PUT

PATCH Publish

PATCH Activate / Deactivate

No Delete endpoint.

Business Rule

Publishing one year automatically unpublishes every other year.

Exactly one year must always be Current.

---

## Admission Cutoffs

GET

GET by Id

POST

PUT

DELETE

Filtering

Search

Pagination

Sorting

Admission Cutoff records may be deleted.

---

## Import

POST

/api/admin/admission-years/{yearId}/import

Accepted file types

xlsx

csv

Multipart/form-data

Response

Validation Report

or

Import Summary

---

# 27. Standard API Response

Successful Response

{
"success": true,
"message": "Operation completed successfully.",
"data": { }
}

Validation Error

{
"success": false,
"message": "Validation failed.",
"errors": [
{
"field": "Faculty",
"message": "Faculty does not exist."
}
]
}

Unexpected Error

{
"success": false,
"message": "An unexpected error occurred."
}

---

# 28. Security Requirements

Authentication

ASP.NET Core Identity

Password hashing

ASP.NET Identity Password Hasher

HTTPS

Required in Production.

Authorization

Role-based.

Roles

Administrator

No student roles exist.

---

# 29. Logging

The system must log

Authentication failures

Import operations

Import validation failures

Manual edits

Activate and deactivate operations

Publishing admission years

Unexpected exceptions

Sensitive information must never be logged.

Passwords

Access Tokens

Connection Strings

Personal Information

must never appear in logs.

---

# 30. Performance Requirements

The prediction endpoint should return results in less than

500 ms

under normal load.

Importing 10,000 records should complete successfully.

Database queries must use indexes.

N+1 queries are prohibited.

AsNoTracking should be used for read-only queries.

Pagination must always be server-side.

---

# 31. Coding Standards

Backend

ASP.NET Core (.NET)

Entity Framework Core

SQL Server

Clean Architecture

FluentValidation

Frontend

Angular

Standalone Components

Reactive Forms

TypeScript Strict Mode

No jQuery.

No server-side rendering is required.

## Frontend Language

The frontend is entirely Arabic.

The application name displayed to users is **تنسيقك**, loaded from the `appName` field in the configuration API response.

All UI text, labels, navigation, messages, and displayed entity names use Arabic.

Entity display names must use NameAr from the API.

NameEn is stored in the database and returned by the API for administration and import purposes, but it is not displayed in the frontend.

The layout must support right-to-left (RTL) text direction.

No English UI is provided.

No language switching is provided.

No translation logic or hardcoded name mapping is allowed in the frontend.

All display names must come directly from the API.

---

# 32. Project Structure

/src

```
Tansekak.Api

Tansekak.Application

Tansekak.Domain

Tansekak.Infrastructure
```

/tests

```
UnitTests

IntegrationTests
```

/docs

```
PROJECT_SPEC.md

API.md

DATABASE.md
```

---

# 33. Acceptance Criteria

The project is considered complete when:

- The product is branded as **Tansekak** across the public website and administration panel.
- Students can predict eligible faculties using the current admission year.
- Administrators can manage universities, faculties, university–faculty assignments, admission years, and admission cutoffs.
- Every Governorate has Arabic and English names (NameAr and NameEn).
- Every University has Arabic and English names (NameAr and NameEn).
- Every Faculty has Arabic and English names (NameAr and NameEn).
- API responses expose both NameAr and NameEn for all named entities.
- The frontend is entirely Arabic and displays only NameAr for entity names.
- All frontend UI text, labels, and navigation are in Arabic.
- Governorates, universities, faculties, university–faculty assignments, and admission years support Activate / Deactivate only; physical deletion is not available for these entities.
- Admission cutoff records may be physically deleted.
- Excel and CSV imports are fully validated before saving.
- Invalid imports never modify the database.
- Authentication is functional.
- All public endpoints work without authentication.
- Results are correctly categorized into Available, Near, and Not Available.
- Results are sorted by the closest cutoff.
- Pagination works correctly.
- The system is responsive on desktop and mobile.
- The project builds successfully without warnings or runtime errors.

---

# 34. Minimum Viable Product (MVP)

The first release of the system MUST include only the following features.

## Public Website

- Home page
- Academic track selection
- Student score input
- Admission prediction
- Available results
- Near results
- Not Available results
- Search by university name (NameAr)
- Search by faculty name (NameAr)
- Arabic-only user interface
- Load More pagination
- Responsive layout
- Mobile support

---

## Administration Panel

- Login
- Logout
- Dashboard
- Governorate management (NameAr and NameEn)
- University management (NameAr and NameEn)
- Faculty management (NameAr and NameEn)
- University–faculty management
- Admission year management
- Admission cutoff management
- Excel import
- CSV import
- Validation report
- Manual editing
- Publish current admission year

---

## Database

The database must contain

- Governorates
- Universities
- Faculties
- University Faculties
- Admission Years
- Admission Cutoffs

---

## Authentication

ASP.NET Core Identity

Administrator only.

No public registration.

No password reset.

No email verification.

---

## Import

Supported formats

- XLSX
- CSV

Validation is mandatory.

Partial import is prohibited.

---

## Prediction Engine

Input

- Academic Track
- Student Score

Output

- Available
- Near
- Not Available

Sorting

Closest cutoff first.

---

# 35. Future Versions

The following features are intentionally excluded from Version 1.

These features may be implemented later.

---

## Universities

- Private Universities
- National Universities
- Technological Universities

---

## Student Features

- Student Accounts
- Student Profiles
- Saved Results
- Favorite Faculties
- Search History

---

## Faculty Details

- Faculty Description
- Career Information
- Study Duration
- Tuition Fees
- Official Website
- Images

---

## Admission Analysis

- Multi-year comparison
- Admission trends
- Charts
- Statistics
- Cutoff history

---

## Smart Features

- AI recommendations
- Personalized suggestions
- Similar faculties
- Probability estimation

---

## Notifications

- Email notifications
- Push notifications
- SMS notifications

---

## Administration

- Audit Logs
- Activity History
- Multiple Administrators
- Permission Management

---

# 36. Non-Goals

The project is NOT intended to

Predict future admission cutoffs.

Estimate next year's admission.

Replace the official admission system.

Collect student personal information.

Provide career counseling.

Recommend universities using AI.

The project only compares the student's score against official admission data.

---

# 37. Assumptions

The following assumptions are considered true.

- Official admission data is accurate.
- Administrators upload verified files.
- There is exactly one Current Admission Year.
- Universities, faculties, and university–faculty assignments are managed by administrators.
- Every Governorate, University, and Faculty stores both Arabic and English names.
- Students always enter scores using the current year's maximum score.

---

# 38. Success Criteria

The project is considered successful when

- Students can obtain accurate admission results within a few seconds.
- Administrators can update yearly admission data without code changes.
- A new admission year can be added using only Excel or CSV.
- Switching the Current admission year requires no deployment.
- Invalid imports never affect production data.
- The application remains maintainable for future admission years.
- The entire frontend is in Arabic, including all UI text and displayed entity names.

---

# 40. Development Principles

The implementation MUST follow these principles.

- Clean Architecture.
- Domain-driven naming.
- SOLID principles.
- Dependency Injection.
- Repository pattern is NOT required unless there is a real need.
- Entity Framework Core should be used directly through the application's DbContext abstraction.
- Validation must use FluentValidation.
- Business logic must never exist inside Controllers.
- Controllers must remain thin.
- Services must contain business logic.
- Database constraints must enforce data integrity whenever possible.
- No hardcoded values.
- Configuration must be stored in configuration files or the database.
- Every public API must return a consistent response model.
- Every endpoint must support cancellation using CancellationToken.
- Every asynchronous operation must use async/await.
- Read-only queries should use AsNoTracking().
- All database operations must support transactions when required.
- Exceptions must be handled centrally using a Global Exception Handler.
- Structured logging must be used.
- Sensitive data must never be logged.

---

# 41. Final Notes

This document is the single source of truth for **Tansekak**.

If implementation details conflict with this specification, this specification takes precedence.

No functionality may be implemented based on assumptions.

Any missing requirement must be clarified before implementation.

The implementation must prioritize simplicity, maintainability, correctness, and long-term extensibility over unnecessary complexity.
