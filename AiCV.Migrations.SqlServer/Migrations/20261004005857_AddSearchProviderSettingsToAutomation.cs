using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCV.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchProviderSettingsToAutomation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SearchProviderSettingsJson",
                table: "AutomationSettings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SearchProviderSettingsJson",
                table: "AutomationSettings");
        }
    }
}
