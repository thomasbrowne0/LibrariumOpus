using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librarium.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOldISBNColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the old ISBN column
            migrationBuilder.DropColumn(
                name: "ISBN",
                table: "Books");

            // Rename ISBNNew to ISBN
            migrationBuilder.RenameColumn(
                name: "ISBNNew",
                table: "Books",
                newName: "ISBN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rename ISBN back to ISBNNew
            migrationBuilder.RenameColumn(
                name: "ISBN",
                table: "Books",
                newName: "ISBNNew");

            // Recreate the old ISBN column
            migrationBuilder.AddColumn<string>(
                name: "ISBN",
                table: "Books",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }
    }
}
