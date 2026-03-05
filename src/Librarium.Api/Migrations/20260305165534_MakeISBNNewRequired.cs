using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librarium.Api.Migrations
{
    /// <inheritdoc />
    public partial class MakeISBNNewRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill ISBNNew from ISBN before making it required
            migrationBuilder.Sql(@"
                UPDATE ""Books"" 
                SET ""ISBNNew"" = ""ISBN"" 
                WHERE ""ISBNNew"" IS NULL;
            ");

            migrationBuilder.AlterColumn<string>(
                name: "ISBNNew",
                table: "Books",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ISBNNew",
                table: "Books",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
        }
    }
}
