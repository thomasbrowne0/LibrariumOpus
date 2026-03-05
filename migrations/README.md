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

## V3 - AddPhoneNumberToMembers

**Date:** March 5, 2026  
**Migration File:** `V3__AddPhoneNumberToMembers.sql`  
**EF Migration:** `20260305162700_AddPhoneNumberToMembers`

### Description
Adds a phone number field to the Members table. This field is initially nullable to allow existing member records to remain valid during the transition period.

### Type of Change
**Additive (Non-Breaking)** - Adds an optional column without affecting existing data or requiring immediate changes.

### Schema Changes

**Column Added:**
- `PhoneNumber` (varchar 20, nullable) to Members table

### API Impact

**Endpoint Behavior Changes:**
- `GET /api/members` - Response schema expanded to include optional `phoneNumber` field

**Response Contract Changes:**

*Previous Member Response (V2):*
```json
{
  "memberId": 1,
  "firstName": "Anna",
  "lastName": "Berg",
  "email": "anna@example.com"
}
```

*New Member Response (V3):*
```json
{
  "memberId": 1,
  "firstName": "Anna",
  "lastName": "Berg",
  "email": "anna@example.com",
  "phoneNumber": null
}
```

**Breaking Changes:** None - adding an optional field is non-breaking for REST clients.

### Deployment Notes

**Order of Operations:**
1. Apply database migration (adds nullable column)
2. Deploy application code
3. No immediate data entry required

**Zero-Downtime:** Yes

**Deployment Window Behavior:**
- **Migration applied, old code running:** New column exists but unused. No impact.
- **New code deployed:** API returns `phoneNumber` field (null for existing members).

**Rollback Strategy:**
- Revert application code
- Drop column: `ALTER TABLE "Members" DROP COLUMN "PhoneNumber";`

### Decisions and Tradeoffs

**Nullable Initially:**
Made PhoneNumber nullable in this first migration rather than immediately required. This is a multi-step migration strategy to avoid breaking existing data. Existing member records don't have phone numbers, and requiring this field immediately would either require a mass data entry effort or prevent the migration from applying. A subsequent migration (V5) will backfill default values and make this field required after stakeholders have had time to populate real phone numbers. This staged approach reduces risk and allows gradual data quality improvement.

**Field Length:**
Chose `varchar(20)` to accommodate various phone number formats including country codes, extensions, and formatting characters (e.g., "+1 (555) 123-4567"). International phone numbers can be up to 15 digits plus formatting. A 20-character limit provides flexibility without excessive storage overhead. No validation logic applied at database level - validation happens in application layer to accommodate different regional formats.

---

## V3.5 - CleanupDuplicateEmails (Manual Script)

**Date:** March 5, 2026  
**Migration File:** `V3.5__CleanupDuplicateEmails.sql`  
**Type:** Manual Data Cleanup Script (Not an EF Migration)

### Description
This is a **manual cleanup script** that must be run before applying V4. It identifies and resolves duplicate email addresses in the Members table, which would otherwise cause V4's unique constraint to fail.

### Type of Change
**Destructive** - Modifies or removes existing data based on business rules.

### Purpose

The script provides two strategies for resolving duplicate emails:

**Strategy 1: Merge Duplicates (Recommended)**
- Keeps the oldest member account (lowest ID)
- Reassigns all loans from duplicate accounts to the kept account
- Deletes duplicate member records
- Preserves all loan history

**Strategy 2: Disambiguate Email Addresses**
- Keeps all member records
- Adds suffix to duplicate emails (e.g., `email@example.com_duplicate_1`)
- No data loss, but creates "fake" email addresses

### Deployment Notes

**CRITICAL:** This script must be executed during a maintenance window BEFORE applying V4__EnforceEmailUniqueness.sql.

**Order of Operations:**
1. Run identification queries to review duplicates
2. Stakeholders decide which merge strategy to use
3. Execute chosen resolution strategy (uncomment relevant section)
4. Verify no duplicates remain
5. Only then apply V4 migration

**Risk:** If duplicates exist when V4 is applied, the unique constraint creation will fail and block the migration.

### Decisions and Tradeoffs

**Manual Script vs. Automated:**
Chose a manual script over automatic resolution because merging member accounts is a business decision requiring human judgment. Different scenarios may require different resolutions: some duplicates might be legitimate separate accounts (e.g., family members sharing an email), others might be data entry errors. Providing multiple strategies and requiring manual review ensures data accuracy and prevents unintended data loss.

**Merge Strategy Selection:**
Recommended "keep oldest, reassign loans" approach based on common patterns: the first account created is typically the "real" one, and subsequent duplicates are often accidental recreations. This preserves all transactional history while cleaning up duplicate identities. Alternative email disambiguation strategy provides a safety net for organizations uncomfortable with deleting member records, though it creates operational challenges (invalid email addresses).

---

## V4 - EnforceEmailUniqueness

**Date:** March 5, 2026  
**Migration File:** `V4__EnforceEmailUniqueness.sql`  
**EF Migration:** `20260305162825_EnforceEmailUniqueness`

### Description
Adds a unique index on the Email column of the Members table, enforcing that each email address can only be used once. This supports the business requirement for email as the login identifier.

### Type of Change
**Requires Coordination** - Non-breaking if applied correctly, but requires careful sequencing.

### Schema Changes

**Index Created:**
- Unique index `IX_Members_Email` on Members(Email)

**Prerequisites:**
- All duplicate emails must be resolved (via V3.5 cleanup script)

### API Impact

**No API Contract Changes** - Endpoints remain unchanged.

**Behavioral Changes:**
- Creating or updating a member with a duplicate email will now fail
- Application must handle unique constraint violations gracefully
- Error response example:
```json
{
  "error": "Email address already in use",
  "code": "DuplicateEmail"
}
```

**Breaking Changes:** None at API level, but client applications must handle new error conditions.

###Deployment Notes

**Order of Operations:**
1. **FIRST:** Run V3.5 cleanup script to resolve duplicate emails
2. Verify no duplicates remain
3. Apply V4 migration (creates unique index)
4. Deploy application code with error handling for duplicate emails
5. Update frontend validation to check email uniqueness

**Zero-Downtime:** Yes, if prerequisites met.

**Deployment Window Behavior:**
- **Migration applied, old code running:** Duplicate emails rejected at database level, may cause generic errors. Not ideal but not catastrophic.
- **New code deployed:** Proper error handling for duplicate email attempts.

**Index Creation Performance:**
- PostgreSQL will scan entire Members table to verify uniqueness
- `CREATE UNIQUE INDEX` can be slow on large tables (100K+ rows)
- Consider using `CREATE UNIQUE INDEX CONCURRENTLY` for production (manual modification of SQL script)
- Locks table briefly during index creation

**Rollback Strategy:**
- Drop index: `DROP INDEX "IX_Members_Email";`
- Revert application code if duplicate email handling was deployed

### Decisions and Tradeoffs

**Unique Index vs. Unique Constraint:**
Implemented as a unique index rather than a `UNIQUE` constraint. Both provide the same guarantee, but indexes offer better query performance for lookups by email (login queries). The index serves dual purposes: enforcing uniqueness and optimizing authentication queries. Functionally equivalent to a unique constraint but with performance benefits.

**Multi-Step Migration Approach:**
Split email uniqueness enforcement into separate migration (V4) from phone number addition (V3). This allows independent deployment timelines if needed. If unique constraint creation fails due to duplicates, it doesn't impact the phone number addition. Smaller migrations are easier to test and rollback. The tradeoff is more migrations to track, but improved safety and flexibility justify this.

**No Automatic Conflict Resolution:**
Deliberately avoided automatic duplicate resolution in the migration itself. PostgreSQL could automatically delete duplicates or update emails, but this risks data loss or corruption. Requiring manual V3.5 script execution ensures human review of duplicate resolution. Migrations should be deterministic and reversible; automatic deletion is neither. Safety over convenience.

**Concurrent Index Consideration:**
Default migration uses blocking `CREATE UNIQUE INDEX`. For production deployment on large tables, the SQL script can be manually modified to `CREATE UNIQUE INDEX CONCURRENTLY`, which takes longer but doesn't lock the table. Trade-off: migration time vs. availability. For most library systems (under 100K members), blocking index creation is acceptably fast (<5 seconds). Documented as an option for larger deployments.

---

## V5 - MakePhoneNumberRequired

**Date:** March 5, 2026  
**Migration File:** `V5__MakePhoneNumberRequired.sql`  
**EF Migration:** `20260305162919_MakePhoneNumberRequired`

### Description
Makes the PhoneNumber field mandatory in the Members table. This completes the transition started in V3, after stakeholders have had an opportunity to populate real phone numbers.

### Type of Change
**Requires Coordination** - Modifies existing data and schema constraints.

### Schema Changes

**Column Modified:**
- `PhoneNumber` changed from nullable to NOT NULL
- Default value of `'000-000-0000'` applied to existing NULL values via backfill

**Data Migration:**
- All existing members with NULL phone numbers are updated to `'000-000-0000'`
- This is a placeholder value indicating phone number needs to be collected

### API Impact

**Endpoint Behavior Changes:**
- `GET /api/members` - `phoneNumber` field now always present (never null)
- `POST /api/members` - Creating members without phone number will fail
- `PUT /api/members` - Updating members to remove phone number will fail

**Response Contract Changes:**

*Previous Member Response (V3-V4):*
```json
{
  "memberId": 1,
  "firstName": "Anna", 
  "lastName": "Berg",
  "email": "anna@example.com",
  "phoneNumber": null
}
```

*New Member Response (V5):*
```json
{
  "memberId": 1,
  "firstName": "Anna",
  "lastName": "Berg",
  "email": "anna@example.com",
  "phoneNumber": "000-000-0000"
}
```

**Breaking Changes:**
- **POST/PUT requests must now include `phoneNumber`** - clients that don't send this field will receive validation errors
- Significant breaking change for write operations
- Consider adding API deprecation warnings in V3/V4 to prepare clients

### Deployment Notes

**Order of Operations:**
1. Ensure stakeholders have populated phone numbers for critical members
2. Apply V5 migration (backfills defaults, makes field required)
3. Deploy application code with mandatory phone number validation
4. Update frontend forms to require phone number input
5. Communicate breaking change to API consumers

**Zero-Downtime:** Partial - migration can be applied with old code, but full functionality requires coordinated deployment.

**Deployment Window Behavior:**
- **Migration applied, old code running:** Database enforces NOT NULL, but old application doesn't send phone numbers. New member creation fails. **High risk window.**
- **New code with validation, old database (nullable):** Application sends phone numbers, database accepts. **Safe window.**

**Recommended Deployment Order:**
1. Deploy application code that handles phone as required (but DB still accepts NULL)
2. Allow stabilization period (monitor for issues)
3. Apply V5 migration to enforce NOT NULL at database level
4. Migration becomes insurance against application bugs

**Rollback Strategy:**
- If immediate rollback needed, revert to nullable: `ALTER TABLE "Members" ALTER COLUMN "PhoneNumber" DROP NOT NULL;`
- Application rollback straightforward (remove required validation)
- Data remains intact (default values persist but become nullable again)

### Decisions and Tradeoffs

**Three-Step Migration Strategy (V3 → V3.5 → V4 → V5):**
The phone number implementation was deliberately split across multiple migrations rather than a single "add phone required" migration. This staged approach provides several benefits: (1) V3 allows column creation without breaking existing data; (2) Time between V3 and V5 gives stakeholders opportunity to populate legitimate phone numbers; (3) If V5 is delayed, system still functions with optional phone numbers; (4) Each migration is independently testable and rollback-able. Alternative single-migration approach would require either blocking deployment until all phone numbers collected (operationally impractical) or losing phone number collection opportunity. Staging reduces risk at cost of more migrations to manage.

**Default Value Choice:**
Used `'000-000-0000'` as default phone number rather than empty string or other sentinel. This format: (1) Clearly invalid for actual dialing (won't be misused); (2) Visually distinctive (easy to identify incomplete data); (3) Meets NOT NULL constraint; (4) Valid phone number format (passes basic regex validation). Alternative empty string considered but rejected - empty phone numbers are semantically confusing (does member have no phone or unknown phone?). Alternative "UNKNOWN" rejected - not a valid phone format. The placeholder approach makes data quality issues visible while maintaining technical validity.

**Backfill Before Constraint:**
Migration explicitly backfills NULL values with default BEFORE altering column to NOT NULL. This ensures migration won't fail due to existing NULL values. Alternative approach of adding NOT NULL with DEFAULT clause works in some databases but PostgreSQL requires explicit backfill for existing rows. Manual backfill provides more control and visibility into how many records are affected. SQL statement logged in migration output for audit purposes.

**Deployment Window Risk:**
Mitigated "migration before code" deployment risk by recommending inverse deployment order (code first, then migration). This is counter to usual "database changes first" pattern. Justification: Adding validation is safer than enforcing constraint. Application can gracefully handle phone collection, but database constraint causes hard failures. For write-heavy operations like member registration, application-level flexibility is more important than database-level enforcement during transition. Once code is stable, database constraint provides long-term insurance.

**No Phone Number Validation at DB Level:**
Deliberately avoided CHECK constraints for phone number format (e.g., regex pattern). Phone number formats are highly variable internationally: country codes, extensions, special characters, lengths. Database-level validation would be either too strict (rejecting valid international formats) or too loose (not actually validating). Validation logic belongs in application layer where it can be sophisticated, configurable, and easily updated. Database enforces presence (NOT NULL) and length (20 characters), application enforces format.

---

## V6 - AddStatusToLoans

**Date:** March 5, 2026  
**Migration File:** `V6__AddStatusToLoans.sql`  
**EF Migration:** `20260305164032_AddStatusToLoans`

### Description
Adds a status tracking field to the Loans table to indicate the current state of a loan (Active, Returned, Overdue). This field is initially nullable to allow existing loan records to remain valid during the transition period.

### Type of Change
**Additive (Non-Breaking)** - Adds an optional column without affecting existing data or requiring immediate changes.

### Schema Changes

**Enum Added:**
- `LoanStatus` enum: Active (0), Returned (1), Overdue (2)

**Column Added:**
- `Status` (integer, nullable) to Loans table

### API Impact

**API Versioning Introduced:**
- **API v1:** `/api/loans` - Continues using ReturnDate for backward compatibility (no Status field)
- **API v2:** `/api/v2/loans` - Exposes new Status field alongside ReturnDate

**v1 Endpoint Behavior (Unchanged):**
- `GET /api/loans/{memberId}` - Returns loans without Status field
- `POST /api/loans` - Creates loans, Status set internally but not exposed

**v2 Endpoint Behavior (New):**
- `GET /api/v2/loans/{memberId}` - Returns loans with Status field
- `POST /api/v2/loans` - Creates loans, Status exposed in response

**Response Contract Changes:**

*v1 Response (Backward Compatible):*
```json
{
  "loanId": 1,
  "bookTitle": "The Great Gatsby",
  "loanDate": "2026-03-01T10:00:00Z",
  "returnDate": null
}
```

*v2 Response (New):*
```json
{
  "loanId": 1,
  "bookTitle": "The Great Gatsby",
  "loanDate": "2026-03-01T10:00:00Z",
  "returnDate": null,
  "status": 0
}
```

**Breaking Changes:** None - existing v1 clients unaffected by new v2 endpoints.

### Deployment Notes

**Order of Operations:**
1. Apply database migration (adds nullable column)
2. Deploy application code with v2 endpoints
3. Communicate v2 API availability to consumers
4. No immediate data entry required

**Zero-Downtime:** Yes

**Deployment Window Behavior:**
- **Migration applied, old code running:** New column exists but unused. No impact.
- **New code deployed:** v1 API unchanged, v2 API available with Status field (null for existing loans).

**Rollback Strategy:**
- Revert application code (removes v2 endpoints)
- Drop column: `ALTER TABLE "Loans" DROP COLUMN "Status";`

**API Versioning Strategy:**
- v1 endpoints maintain backward compatibility indefinitely
- v2 endpoints expose enhanced functionality
- Both versions coexist, allowing gradual client migration
- Future: v1 can be deprecated after transition period (e.g., 6-12 months)

### Decisions and Tradeoffs

**API Versioning Approach:**
Chose URL-based versioning (`/api/loans` vs `/api/v2/loans`) over header-based or query parameter versioning. URL versioning provides: (1) Clear visibility - clients explicitly choose their version; (2) Easy testing - can curl/browse different versions directly; (3) Caching friendly - different URLs allow different cache policies; (4) Documentation clarity - Swagger/OpenAPI can document versions separately. Tradeoff: More controller code (separate v1/v2 controllers or methods). Alternative header-based versioning (`Accept: application/vnd.librarium.v2+json`) is more RESTful but less discoverable and harder for non-technical consumers. URL versioning prioritizes pragmatism over REST purity.

**Status as Integer Enum:**
Stored Status as integer (0, 1, 2) rather than string ("Active", "Returned", "Overdue"). Benefits: (1) Smaller storage (4 bytes vs ~8 bytes); (2) Faster comparisons and indexing; (3) Type safety in C# with enum; (4) Easy to add new statuses (e.g., Lost = 3) without migration. Tradeoff: Database queries less readable (`WHERE Status = 0` vs `WHERE Status = 'Active'`). Application layer handles enum-to-string conversion for API responses, keeping database efficient while API remains human-readable.

**Nullable Initially:**
Made Status nullable in V6 rather than immediately required, following expand-contract pattern from Phase 3. Existing loan records don't have status, and deriving status from ReturnDate can be done in a controlled manner in V7. This two-step approach: (1) Allows migration to succeed on existing data; (2) Provides testing window before making required; (3) Enables gradual status population if needed. V7 will backfill and make required.

**Parallel v1/v2 APIs:**
Maintained both v1 (ReturnDate-based) and v2 (Status-based) APIs rather than forcing immediate migration. This allows: (1) Existing integrations continue working; (2) New clients adopt enhanced Status field; (3) Gradual migration path; (4) Testing new functionality without risk. Tradeoff: More code to maintain two versions. Alternative breaking change (remove ReturnDate, force Status) would simplify code but break all existing clients. Backward compatibility chosen over simplicity.

**Status vs ReturnDate Relationship:**
Status and ReturnDate are related but serve different purposes: (1) ReturnDate records when book was physically returned (audit trail); (2) Status indicates current state (business logic). A loan can be marked Overdue (status) while ReturnDate is still null. Status provides richer semantics than binary "returned or not" from ReturnDate. Future enhancement: automated status updates (e.g., cron job marks loans overdue based on loan date + policy duration). Keeping both fields provides flexibility for business logic evolution.

---

## V7 - MakeLoanStatusRequired

**Date:** March 5, 2026  
**Migration File:** `V7__MakeLoanStatusRequired.sql`  
**EF Migration:** `20260305164131_MakeLoanStatusRequired`

### Description
Makes the Status field mandatory in the Loans table. This completes the transition started in V6, with intelligent backfilling that derives status from the existing ReturnDate field.

### Type of Change
**Requires Coordination** - Modifies existing data and schema constraints.

### Schema Changes

**Column Modified:**
- `Status` changed from nullable to NOT NULL
- Default value of `0` (Active) applied to new records via schema default

**Data Migration:**
- Existing loans backfilled with Status derived from ReturnDate:
  - `ReturnDate IS NULL` → `Status = 0 (Active)`
  - `ReturnDate IS NOT NULL` → `Status = 1 (Returned)`

### API Impact

**Endpoint Behavior Changes:**
- `GET /api/loans/{memberId}` (v1) - No change, Status still not exposed
- `GET /api/v2/loans/{memberId}` (v2) - Status field now always present (never null)
- `POST /api/loans` (v1) - Creates loans with Status = Active internally
- `POST /api/v2/loans` (v2) - Creates loans with Status = Active, exposed in response

**Response Contract Changes:**

*v2 Previous Response (V6):*
```json
{
  "loanId": 1,
  "bookTitle": "The Great Gatsby",
  "loanDate": "2026-03-01T10:00:00Z",
  "returnDate": null,
  "status": null
}
```

*v2 New Response (V7):*
```json
{
  "loanId": 1,
  "bookTitle": "The Great Gatsby",
  "loanDate": "2026-03-01T10:00:00Z",
  "returnDate": null,
  "status": 0
}
```

**Breaking Changes:**
- **v2 API:** Status field changes from nullable to non-nullable (minor breaking change for strict typing)
- **v1 API:** No breaking changes (Status still not exposed)

### Deployment Notes

**Order of Operations:**
1. Apply V7 migration (backfills Status, makes field required)
2. Application code already handles required Status (deployed in V6)
3. Monitor v2 API responses to verify Status populated correctly

**Zero-Downtime:** Yes - application code from V6 already sets Status on new loans.

**Deployment Window Behavior:**
- **Migration applied, V6 code running:** Database has all Status values, code creates new loans with Status. **Safe.**
- **V7 code deployed (same as V6 for this migration):** No code changes needed, Status already handled. **Safe.**

**Backfill Logic:**
- Migration uses CASE statement to intelligently derive Status:
  ```sql
  UPDATE "Loans" 
  SET "Status" = CASE 
      WHEN "ReturnDate" IS NULL THEN 0  -- Active
      ELSE 1                             -- Returned
  END 
  WHERE "Status" IS NULL;
  ```
- Does not set Overdue status during backfill (requires date calculation and policy knowledge)
- Future enhancement: scheduled job to update Active → Overdue based on loan duration policy

**Rollback Strategy:**
- If immediate rollback needed: `ALTER TABLE "Loans" ALTER COLUMN "Status" DROP NOT NULL;`
- Application code unchanged (already handles Status from V6)
- Data remains intact (backfilled values persist but become nullable again)

### Decisions and Tradeoffs

**Two-Step Migration (V6 → V7):**
Split Status implementation across two migrations following expand-contract pattern. V6 added nullable Status, V7 makes it required. Benefits: (1) V6 migration guaranteed to succeed (no constraint violations); (2) Testing period between V6 and V7 to verify Status logic; (3) Independent rollback points; (4) Gradual schema evolution. Alternative single-step migration would be simpler but riskier - if Status logic has bugs, easier to catch with nullable phase first. Cost: two migrations instead of one, but safety justifies overhead.

**Intelligent Backfill from ReturnDate:**
Derived Status from existing ReturnDate rather than defaulting all to Active. Logic: loans with ReturnDate are clearly Returned, loans without are Active. This preserves historical accuracy for existing data. Alternative naive approach (set all to Active) would corrupt historical data - returned loans would appear active. Backfill accuracy critical for reporting and analytics. Only limitation: cannot detect Overdue from static data (requires current date comparison and policy rules).

**No Overdue Detection in Migration:**
Backfill does not set Status = Overdue for any loans, only Active or Returned. Rationale: (1) Overdue requires business logic (loan duration policy, grace periods); (2) Overdue depends on current date (migration should be time-independent); (3) Overdue rules may vary by member type or book type. Safer to default to Active/Returned and let application layer or scheduled job apply Overdue logic based on current policy. Future: implement background job that periodically scans Active loans and updates to Overdue based on configurable rules.

**Schema Default vs Application Default:**
Migration adds `DEFAULT 0` at database level for new records, ensuring Status = Active even if application layer fails to set it. This provides defense-in-depth: (1) Application sets Status explicitly; (2) Database default as fallback; (3) NOT NULL constraint prevents gaps. Layered approach reduces risk of missing Status values. Application layer remains primary authority (can override default), database constraint acts as safety net.

**Backward Compatibility Preservation:**
v1 API continues to not expose Status field even after V7. This maintains perfect backward compatibility - existing clients see zero changes. Only clients explicitly opting into v2 API see Status. This allows: (1) Legacy integrations run indefinitely; (2) New clients get enhanced features; (3) Migration to v2 on client timeline, not forced. Tradeoff: maintaining two API versions adds complexity. Plan: monitor v1 usage, deprecate after 12-month transition period (announce in V8, remove in V9).

**Status Update Responsibility:**
Migration makes Status required but does not implement status transition logic (e.g., Active → Overdue, Active → Returned). Status updates remain application responsibility. When a loan is returned, application must: (1) Set ReturnDate; (2) Set Status = Returned. Keeping logic in application allows: (1) Complex business rules (e.g., grace periods); (2) Audit logging of status changes; (3) Validation and permissions; (4) Easy rule updates without migration. Database enforces data integrity (NOT NULL), application enforces business rules.

**Future Enhancements:**
- Implement scheduled job for Overdue detection (e.g., daily scan of Active loans)
- Add Status transition logging for audit trail (StatusHistory table)
- Introduce additional statuses: Lost (3), Renewed (4), Cancelled (5)
- API endpoint for manual status updates with validation
- Reporting dashboard showing loan status distribution
- Email notifications on status changes (especially Overdue)

---

## Migration History Summary

| Version | Date | Type | Description |
|---------|------|------|-------------|
| V1 | 2026-03-05 | Additive (Non-Breaking) | Initial schema: Books, Members, Loans |
| V2 | 2026-03-05 | Additive (Potentially Breaking) | Added Authors and BookAuthors tables with many-to-many relationship |
| V3 | 2026-03-05 | Additive (Non-Breaking) | Added optional PhoneNumber column to Members |
| V3.5 | 2026-03-05 | Destructive (Manual) | Data cleanup script for duplicate email addresses |
| V4 | 2026-03-05 | Requires Coordination | Added unique index on Members.Email |
| V5 | 2026-03-05 | Requires Coordination | Made PhoneNumber required with default backfill |
| V6 | 2026-03-05 | Additive (Non-Breaking) | Added optional Status column to Loans with API v2 versioning |
| V7 | 2026-03-05 | Requires Coordination | Made Status required with intelligent backfill from ReturnDate |

---

## Notes

- All migrations are generated using EF Core and manually exported to SQL
- SQL scripts are idempotent where possible (using `IF NOT EXISTS` checks)
- Migration naming convention: `V{number}__{description}.sql`
- EF migration naming: `YYYYMMDDHHMMSS_{description}`
