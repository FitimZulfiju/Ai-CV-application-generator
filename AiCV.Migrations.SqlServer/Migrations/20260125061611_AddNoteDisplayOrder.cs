using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCV.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddNoteDisplayOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "Notes",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "Notes");
        }
    }
}
