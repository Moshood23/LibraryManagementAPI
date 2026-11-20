# Library Management API

[![CI](https://github.com/Moshood23/LibraryManagementAPI/actions/workflows/ci.yml/badge.svg?branch=Master)](https://github.com/Moshood23/LibraryManagementAPI/actions/workflows/ci.yml)

ASP.NET Core 8 Web API for managing books, authors and genres, with ASP.NET Identity and JWT authentication.

## Run locally

Requirements: .NET 8 SDK and SQL Server (the default connection string uses `localhost` with Windows auth; change `ConnectionStrings:DefaultConnection` if needed).

```bash
cd LibraryManagementAPI
dotnet run --launch-profile https
```

Migrations are applied automatically at startup. Swagger UI is at `/swagger` in Development.

In Development, `appsettings.Development.json` provides a throwaway JWT key and seeds an admin user
`admin@library.com` / `Admin@123`. In any other environment you must supply real values:

```bash
dotnet user-secrets set "JWT:Secret" "<at least 32 random characters>"
dotnet user-secrets set "SeedAdmin:Password" "<strong password>"
```

(or the `JWT__Secret` / `SeedAdmin__Password` environment variables). The app refuses to start without a JWT secret.

## Endpoints (v1)

| Method | Route | Auth |
| --- | --- | --- |
| POST | `/api/v1/Auth/register`, `/api/v1/Auth/login` | – |
| GET | `/api/v1/Books` (filters below), `/api/v1/Books/{id}`, `/api/v1/Books/search?term=` | – |
| GET | `/api/v1/Authors` (paged), `/api/v1/Authors/{id}`, `/api/v1/Authors/search?term=` | – |
| GET | `/api/v1/Genres` (paged), `/api/v1/Genres/{id}`, `/api/v1/Genres/search?term=`, `/api/v1/Genres/popular?top=` | – |
| POST / PUT / DELETE | `/api/v1/Books`, `/api/v1/Authors`, `/api/v1/Genres` | Admin |
| POST | `/api/v1/Books/{id}/borrow` | Signed in |
| GET | `/api/v1/Loans/me?includeReturned=`, `/api/v1/Loans/{id}` | Signed in (own loans) |
| POST | `/api/v1/Loans/{id}/return` | Signed in (own loans), Admin (any) |
| GET | `/api/v1/Loans?status=Active\|Overdue\|Returned\|All`, `/api/v1/Loans/overdue` | Admin |

Deletes are soft deletes. Authors and genres that still have books can't be deleted (409).

### Browsing: filters, sorting and paging

`GET /api/v1/Books`, `/api/v1/Authors` and `/api/v1/Genres` return one page at a time:

```json
{ "items": [...], "page": 1, "pageSize": 20, "totalCount": 57, "totalPages": 3, "hasPreviousPage": false, "hasNextPage": true }
```

| Parameter | Books | Authors | Genres |
| --- | --- | --- | --- |
| `page`, `pageSize` (1–100, default 20) | ✓ | ✓ | ✓ |
| `search` (case-insensitive) | title, ISBN, description, author, genre | first/last name, bio | name, description |
| `authorId`, `genreId` | ✓ | | |
| `yearFrom`, `yearTo` (inclusive) | ✓ | | |
| `available` (`true` = a copy is on the shelf, `false` = none) | ✓ | | |
| `sortBy` | `Title` (default), `Year`, `Author`, `Newest` | `LastName` (default), `FirstName`, `BookCount`, `Newest` | `Name` (default), `BookCount`, `Newest` |
| `sortDirection` | `Asc` (default), `Desc` | same | same |

Filters combine with AND, e.g. `/api/v1/Books?genreId=…&yearFrom=1980&available=true&sortBy=Year&sortDirection=Desc`. Invalid values (unknown `sortBy`, `yearFrom` after `yearTo`, `pageSize` over 100) return 400 with the reason.

### Lending rules

- Each book has `TotalCopies` (set by admins) and `AvailableCopies` (copies on the shelf).
- A loan lasts `Loans:LoanPeriodDays` (default 14). A member can have at most `Loans:MaxActiveLoansPerUser` (default 5) books out, can't borrow the same book twice at once, and can't borrow anything while they have an overdue book.
- Borrowing the last copy at the same moment as someone else is safe: only one succeeds, the other gets 409.
- A book with copies on loan can't be deleted, and its `TotalCopies` can't be set below the number on loan.

### Rules and errors

- **ISBN** must be a valid ISBN-10 or ISBN-13 (check digit verified). Hyphens and spaces are accepted and stripped before saving.
- **ISBNs are unique** among active books, and **genre names are unique** (case-insensitive). Duplicates return 409.
- **Every error uses the same shape**, including validation failures, 401, 403 and unexpected server errors:

```json
{ "success": false, "message": "Validation failed", "data": null, "errors": ["ISBN: ISBN must be a valid ISBN-10 or ISBN-13."] }
```

## Tests

```bash
dotnet test
```

The integration tests run the whole API against an in-memory database, so no SQL Server is needed.

## Continuous integration

`.github/workflows/ci.yml` restores, builds (warnings fail the build) and runs the tests on every push and pull request to `Master` and `Development`. Test results are uploaded as a `test-results` artifact on each run.
