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

## Migration History Summary

| Version | Date | Type | Description |
|---------|------|------|-------------|
| V1 | 2026-03-05 | Additive (Non-Breaking) | Initial schema: Books, Members, Loans |

---

## Notes

- All migrations are generated using EF Core and manually exported to SQL
- SQL scripts are idempotent where possible (using `IF NOT EXISTS` checks)
- Migration naming convention: `V{number}__{description}.sql`
- EF migration naming: `YYYYMMDDHHMMSS_{description}`
