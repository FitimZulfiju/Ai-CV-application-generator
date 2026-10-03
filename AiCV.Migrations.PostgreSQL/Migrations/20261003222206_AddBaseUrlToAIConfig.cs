using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCV.Migrations.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddBaseUrlToAIConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BaseUrl",
                table: "UserAIConfigurations",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseUrl",
                table: "UserAIConfigurations");
        }
    }
}
