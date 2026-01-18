using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCV.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectSectionDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SectionDescription",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SectionDescription",
                table: "Projects");
        }
    }
}
