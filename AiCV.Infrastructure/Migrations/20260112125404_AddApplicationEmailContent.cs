using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCV.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationEmailContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationEmailContent",
                table: "GeneratedApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicationEmailContent",
                table: "GeneratedApplications");
        }
    }
}
