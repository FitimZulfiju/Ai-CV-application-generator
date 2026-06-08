using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCV.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddDisplayAsChips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EducationSection_DisplayAsChips",
                table: "CandidateProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ExperienceSection_DisplayAsChips",
                table: "CandidateProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "InterestsSection_DisplayAsChips",
                table: "CandidateProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LanguagesSection_DisplayAsChips",
                table: "CandidateProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ProjectsSection_DisplayAsChips",
                table: "CandidateProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SkillsSection_DisplayAsChips",
                table: "CandidateProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SummarySection_DisplayAsChips",
                table: "CandidateProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EducationSection_DisplayAsChips",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "ExperienceSection_DisplayAsChips",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "InterestsSection_DisplayAsChips",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "LanguagesSection_DisplayAsChips",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "ProjectsSection_DisplayAsChips",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "SkillsSection_DisplayAsChips",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "SummarySection_DisplayAsChips",
                table: "CandidateProfiles");
        }
    }
}
