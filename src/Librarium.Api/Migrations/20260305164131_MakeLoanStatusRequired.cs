using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librarium.Api.Migrations
{
    /// <inheritdoc />
    public partial class MakeLoanStatusRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill Status based on ReturnDate
            // Active (0) if ReturnDate is NULL, Returned (1) if ReturnDate is not NULL
            migrationBuilder.Sql(@"
                UPDATE ""Loans"" 
                SET ""Status"" = CASE 
                    WHEN ""ReturnDate"" IS NULL THEN 0 
                    ELSE 1 
                END 
                WHERE ""Status"" IS NULL;
            ");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Loans",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Loans",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
