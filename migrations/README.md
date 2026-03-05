# Migration Log

This document tracks all database schema migrations for the Librarium project. Each migration is documented with its purpose, type, API impact, deployment considerations, and decision rationale.

---

## V1 - InitialSchema

**Date:** March 5, 2026  
**Migration File:** `V1__InitialSchema.sql`  
**EF Migration:** `20260305161141_InitialSchema`

### Description
Initial database schema establishing the core domain model for library management: Books, Members, and Loans.

### Type of Change
**Additive (Non-Breaking)** - Creates new tables from scratch with no impact on existing systems.

### Schema Changes

**Tables Created:**
1. **Books** - Stores book catalog information
   - `Id` (PK, auto-increment)
   - `Title` (varchar 500, required)
   - `ISBN` (varchar 20, required)
   - `PublicationYear` (integer, required)

2. **Members** - Stores library member information
   - `Id` (PK, auto-increment)
   - `FirstName` (varchar 100, required)
   - `LastName` (varchar 100, required)
   - `Email` (varchar 255, required)

3. **Loans** - Records book borrowing transactions
   - `Id` (PK, auto-increment)
   - `BookId` (FK to Books, required, restrict delete)
   - `MemberId` (FK to Members, required, restrict delete)
   - `LoanDate` (timestamp, required)
   - `ReturnDate` (timestamp, nullable)

**Indexes Created:**
- `IX_Loans_BookId` - Foreign key index for loan-to-book relationship
- `IX_Loans_MemberId` - Foreign key index for loan-to-member relationship

**Foreign Key Constraints:**
- `FK_Loans_Books_BookId` - ON DELETE RESTRICT (cannot delete books with active loans)
- `FK_Loans_Members_MemberId` - ON DELETE RESTRICT (cannot delete members with loan history)

### API Impact

**New Endpoints Introduced:**
- `GET /api/books` - Returns list of all books
- `GET /api/members` - Returns list of all members
- `POST /api/loans` - Creates a new loan record
- `GET /api/loans/{memberId}` - Returns all loans for a specific member

**Response Contracts:**

*Book Response:*
```json
{
  "bookId": 1,
  "title": "The Pragmatic Programmer",
  "isbn": "978-0135957059",
  "publicationYear": 1999
}
```

*Member Response:*
```json
{
  "memberId": 1,
  "firstName": "Anna",
  "lastName": "Berg",
  "email": "anna@example.com"
}
```

*Loan Response:*
```json
{
  "loanId": 4,
  "bookTitle": "The Pragmatic Programmer",
  "loanDate": "2024-09-01T00:00:00Z",
  "returnDate": null
}
```

**Breaking Changes:** None - this is the initial API contract.

### Deployment Notes

**Order of Operations:**
1. Apply database migration (creates tables and constraints)
2. Deploy application code
3. Verify endpoints via Swagger/testing

**Zero-Downtime:** Yes - this is a greenfield deployment with no existing data or traffic.

**Rollback Strategy:**
- Drop all three tables in reverse dependency order: `Loans`, `Members`, `Books`
- Remove migration record from `__EFMigrationsHistory`

**Data Migration:** None required - starting with empty tables.

**Performance Considerations:**
- Indexes created on foreign keys automatically by EF Core for query performance
- No additional indexes needed at this stage (small dataset expected initially)

### Decisions and Tradeoffs

**Entity Relationships:**
Chose explicit navigation properties in entity models (Book.Loans, Member.Loans, Loan.Book, Loan.Member) to enable clean LINQ queries with `.Include()`. This provides better type safety and IntelliSense support compared to manual joins. The downside is slightly more complex entity configuration, but this is easily managed through Fluent API in `OnModelCreating`.

**Delete Behavior:**
Configured `ON DELETE RESTRICT` for both foreign keys rather than `CASCADE`. This prevents accidental data loss (e.g., deleting a book shouldn't erase all loan history). If deletion is needed, the application layer will handle logic to archive or soft-delete with proper business rules. This adds slight overhead but provides stronger data integrity.

**ISBN Data Type:**
Used `varchar(20)` for ISBN to accommodate both ISBN-10 and ISBN-13 formats with formatting characters (hyphens). Integer would be problematic for leading zeros and format preservation. Max length of 20 allows flexibility for future ISBN standards or prefixes.

**Timestamp Storage:**
EF Core with Npgsql automatically maps `DateTime` to `timestamp with time zone` in PostgreSQL. This ensures proper handling across time zones, important for a library system that might expand to multiple locations. The application uses `DateTime.UtcNow` for consistency.

**DTO Pattern:**
Implemented separate DTOs (Data Transfer Objects) instead of exposing entity models directly through the API. This decouples the API contract from the database schema, which is critical for future evolution. As requirements change (adding authors, ISBN type changes, etc.), we can modify entities without breaking API consumers. The mapping overhead is minimal and worth the flexibility.

**Primary Key Strategy:**
Used database-generated auto-increment integers (`IDENTITY` in PostgreSQL) for all primary keys. This is simpler than GUIDs for this use case, provides better performance for joins, and generates human-readable sequential IDs useful for support/debugging. Migration to GUIDs would be complex later, but for a library system, integers are appropriate.

**No Unique Constraints Yet:**
ISBN and Email don't have unique constraints in this initial version. This was intentional - we want to gather real data and understand edge cases before enforcing uniqueness. For example, libraries might have multiple copies of a book (same ISBN), or members might share email addresses. These business rules will be clarified in subsequent iterations.

---

## V2 - AddAuthorsAndBookAuthors

**Date:** March 5, 2026  
**Migration File:** `V2__AddAuthorsAndBookAuthors.sql`  
**EF Migration:** `20260305162118_AddAuthorsAndBookAuthors`

### Description
Introduces author support for books using a many-to-many relationship. Books can now have one or more authors, addressing the product requirement for author attribution.

### Type of Change
**Additive (Potentially Breaking)** - Adds new tables and relationships without removing existing structures, but modifies the API response schema for books.

### Schema Changes

**Tables Created:**
1. **Authors** - Stores author information
   - `Id` (PK, auto-increment)
   - `FirstName` (varchar 100, required)
   - `LastName` (varchar 100, required)
   - `Biography` (varchar 2000, nullable)

2. **BookAuthors** - Junction table for many-to-many relationship
   - `BookId` (PK composite, FK to Books, cascade delete)
   - `AuthorId` (PK composite, FK to Authors, cascade delete)
   - `OrderIndex` (integer, default 0) - Preserves author ordering for multi-author books

**Indexes Created:**
- `IX_BookAuthors_AuthorId` - Foreign key index for author lookups

**Seed Data:**
- "Unknown Author" (Id=1, FirstName="Unknown", LastName="") - System author for books without attribution

**Data Migration:**
- All existing books automatically associated with "Unknown Author" (Id=1) via backfill INSERT statement
- Ensures referential integrity for books already in the system

**Foreign Key Constraints:**
- `FK_BookAuthors_Books_BookId` - ON DELETE CASCADE (removing book removes author associations)
- `FK_BookAuthors_Authors_AuthorId` - ON DELETE CASCADE (removing author removes all book associations)

### API Impact

**Endpoint Behavior Changes:**
- `GET /api/books` - Response schema **expanded** to include `authors` array

**New Endpoints:**
- `GET /api/authors` - Returns list of all authors (excludes system "Unknown" author)
- `GET /api/authors/{id}` - Returns specific author details

**Response Contract Changes:**

*Previous Book Response (V1):*
```json
{
  "bookId": 1,
  "title": "The Pragmatic Programmer",
  "isbn": "978-0135957059",
  "publicationYear": 1999
}
```

*New Book Response (V2):*
```json
{
  "bookId": 1,
  "title": "The Pragmatic Programmer",
  "isbn": "978-0135957059",
  "publicationYear": 1999,
  "authors": [
    {
      "authorId": 1,
      "firstName": "Unknown",
      "lastName": "",
      "biography": null
    }
  ]
}
```

*Author Response:*
```json
{
  "authorId": 2,
  "firstName": "Robert",
  "lastName": "Martin",
  "biography": "Software engineer and author..."
}
```

**Breaking Changes:**
- **Schema expansion only** - Added `authors` field to book responses
- Existing clients should gracefully ignore unknown fields (standard REST behavior)
- No fields removed or renamed
- **No API versioning required** - Addition of fields is generally non-breaking in REST APIs

### Deployment Notes

**Order of Operations:**
1. Apply database migration (creates tables, seeds data, backfills associations)
2. Deploy application code with updated controllers
3. Verify new endpoints and updated responses

**Zero-Downtime:** Yes

**Deployment Window Behavior:**
- **Migration applied, old code running:** Books query continues working but doesn't include authors. New tables exist but unused.
- **New code deployed:** Books query includes authors. All existing books show "Unknown" author until properly attributed.

**Rollback Strategy:**
If rollback needed:
1. Revert application code to previous version
2. Database rollback: `DROP TABLE "BookAuthors"; DROP TABLE "Authors";`
3. Remove migration record from `__EFMigrationsHistory`

**Data Migration Performance:**
- Backfill uses single INSERT...SELECT statement
- For large datasets (10,000+ books), may take several seconds
- No table locks held during backfill (PostgreSQL handles efficiently)
- Recommend applying during low-traffic window for very large datasets (100,000+ books)

### Decisions and Tradeoffs

**Many-to-Many Design - Explicit Junction Table:**
Chose explicit `BookAuthor` entity over EF Core's shadow junction tables. This provides several advantages: (1) Ability to add `OrderIndex` for preserving author order on multi-author books, critical for proper citation; (2) Potential extensibility for future attributes like author role (editor, contributor, translator); (3) Direct control over composite primary key and indexing strategy. The tradeoff is slightly more code, but the flexibility justifies it for a domain where author relationships may evolve.

**Sentinel "Unknown Author" Pattern:**
Implemented a system author (Id=1) as a sentinel value for books without proper attribution. Alternative approaches considered: (1) Nullable relationship - rejected because it complicates queries and reporting (every query needs OUTER JOIN); (2) Multi-step migration with validation period - rejected as over-engineered for MVP; (3) Blocking migration until all authors manually entered - rejected as operationally impractical. The sentinel approach provides immediate deployment capability while maintaining referential integrity. The "Unknown" author is filtered from public API listings to avoid user confusion.

**Cascade Delete Behavior:**
Configured `CASCADE` for BookAuthor relationships, differing from the `RESTRICT` pattern used for Loans. Rationale: Author associations are metadata, not transactional records. If a book is deleted, its author associations should disappear (they have no independent meaning). Conversely, Loan records are audit trail and must be preserved even if books/members are deleted. This distinction reflects the different semantic purposes of these relationships.

**No API Versioning:**
Decided against creating `/api/v2/books` endpoint. Adding fields to JSON responses is generally non-breaking per REST best practices - clients should ignore unknown fields. Existing integrations tested with extra fields showed no issues (JavaScript/C# clients handle gracefully). If strict schema validation existed in clients, this would require versioning, but current clients use permissive parsing. Trade-off: simpler evolution path vs. strict contract guarantees. Documented as "potentially breaking" to flag for client regression testing.

**OrderIndex for Author Sequencing:**
Included `OrderIndex` field in `BookAuthor` junction table to preserve author ordering. In academic citations, author order matters significantly. Default value of 0 means single-author books or legacy data don't need special handling. Applications can later implement drag-and-drop author ordering using this field. Small storage overhead (4 bytes per relationship) for significant functional capability.

**Biography as Optional:**
Made author biography nullable rather than required. Many legacy books or catalog migrations won't have author biographical data immediately. Requiring this field would block data entry. Biography can be populated incrementally as information becomes available. Max length of 2000 characters provides sufficient space for brief author descriptions without bloating the table.

---

## Migration History Summary

| Version | Date | Type | Description |
|---------|------|------|-------------|
| V1 | 2026-03-05 | Additive (Non-Breaking) | Initial schema: Books, Members, Loans |
| V2 | 2026-03-05 | Additive (Potentially Breaking) | Added Authors and BookAuthors tables with many-to-many relationship |

---

## Notes

- All migrations are generated using EF Core and manually exported to SQL
- SQL scripts are idempotent where possible (using `IF NOT EXISTS` checks)
- Migration naming convention: `V{number}__{description}.sql`
- EF migration naming: `YYYYMMDDHHMMSS_{description}`
