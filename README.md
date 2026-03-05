# Librarium - Library Management System Backend

A backend REST API for library management built with .NET 8, Entity Framework Core, and PostgreSQL.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL](https://www.postgresql.org/download/) (version 12 or higher)
- Git

## Setup Instructions

### 1. Clone the Repository

```bash
git clone <repository-url>
cd LibrariumOpus
```

### 2. Configure Database Connection

Update the connection string in `src/Librarium.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=librarium_dev;Username=your_username;Password=your_password"
  }
}
```

### 3. Create the Database

Ensure PostgreSQL is running, then create the database:

```sql
CREATE DATABASE librarium_dev;
```

### 4. Apply Migrations

Navigate to the API project directory and run migrations:

```bash
cd src/Librarium.Api
dotnet ef database update
```

Alternatively, you can apply migrations using the SQL scripts in `/migrations/sql`:

```bash
psql -U postgres -d librarium_dev -f ../../migrations/sql/V1__InitialSchema.sql
```

### 5. Run the Application

```bash
dotnet run
```

The API will be available at:
- HTTPS: `https://localhost:7001`
- HTTP: `http://localhost:5000`
- Swagger UI: `https://localhost:7001/swagger`

## API Endpoints

### Books
- `GET /api/books` - List all books

### Members
- `GET /api/members` - List all members

### Loans
- `POST /api/loans` - Create a new loan
- `GET /api/loans/{memberId}` - Get all loans for a specific member

## Project Structure

```
LibrariumOpus/
├── src/
│   └── Librarium.Api/          # Main API project
│       ├── Controllers/        # API controllers
│       ├── Data/              # DbContext and data access
│       ├── Models/
│       │   ├── Entities/      # Domain entities
│       │   └── DTOs/          # Data transfer objects
│       └── Migrations/        # EF Core migrations
├── migrations/
│   ├── sql/                   # SQL migration scripts
│   └── README.md             # Migration documentation
└── README.md
```

## Development Workflow

### Creating New Migrations

1. Make changes to entity models
2. Create a new migration:
   ```bash
   cd src/Librarium.Api
   dotnet ef migrations add <MigrationName>
   ```
3. Generate the SQL script:
   ```bash
   dotnet ef migrations script --output ../../migrations/sql/V<number>__<description>.sql
   ```
4. Update `/migrations/README.md` with migration documentation

### Git Branching Strategy

- `main` - Production-ready code
- `development` - Integration branch
- `feature/*` - Feature branches (created from `development`)
- `chore/*` - Maintenance tasks (created from `development`)

## Technology Stack

- **.NET 8** - Application framework
- **ASP.NET Core** - Web API framework
- **Entity Framework Core 8** - ORM
- **PostgreSQL** - Database
- **Npgsql** - PostgreSQL provider for EF Core
- **Swagger/OpenAPI** - API documentation

## Database Schema

### Books Table
- `Id` (PK) - Auto-incrementing integer
- `Title` - Book title (max 500 chars)
- `ISBN` - ISBN number (max 20 chars)
- `PublicationYear` - Year of publication

### Members Table
- `Id` (PK) - Auto-incrementing integer
- `FirstName` - Member's first name (max 100 chars)
- `LastName` - Member's last name (max 100 chars)
- `Email` - Member's email address (max 255 chars)

### Loans Table
- `Id` (PK) - Auto-incrementing integer
- `BookId` (FK) - References Books.Id
- `MemberId` (FK) - References Members.Id
- `LoanDate` - Date the book was loaned
- `ReturnDate` - Date the book was returned (nullable)

## Testing the API

Use the Swagger UI at `https://localhost:7001/swagger` or tools like:
- Postman
- cURL
- HTTPie

### Example Requests

**Get all books:**
```bash
curl https://localhost:7001/api/books
```

**Create a loan:**
```bash
curl -X POST https://localhost:7001/api/loans \
  -H "Content-Type: application/json" \
  -d '{"bookId": 1, "memberId": 1}'
```

**Get loans for a member:**
```bash
curl https://localhost:7001/api/loans/1
```

## License

This project is for educational purposes.
