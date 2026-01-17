using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCV.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTagline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tagline",
                table: "CandidateProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tagline",
                table: "CandidateProfiles");
        }
    }
}
