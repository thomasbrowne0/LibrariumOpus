using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librarium.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddISBNNewColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ISBNNew",
                table: "Books",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ISBNNew",
                table: "Books");
        }
    }
}
