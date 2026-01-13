using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCV.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAIModelSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DefaultModel",
                table: "UserSettings",
                newName: "DefaultProvider");

            migrationBuilder.AddColumn<string>(
                name: "DefaultModelId",
                table: "UserSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpenRouterApiKey",
                table: "UserSettings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultModelId",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "OpenRouterApiKey",
                table: "UserSettings");

            migrationBuilder.RenameColumn(
                name: "DefaultProvider",
                table: "UserSettings",
                newName: "DefaultModel");
        }
    }
}
