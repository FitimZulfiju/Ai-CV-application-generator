using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCV.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomSectionHeaders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EducationSection_Icon",
                table: "CandidateProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EducationSection_Title",
                table: "CandidateProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExperienceSection_Icon",
                table: "CandidateProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExperienceSection_Title",
                table: "CandidateProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InterestsSection_Icon",
                table: "CandidateProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InterestsSection_Title",
                table: "CandidateProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LanguagesSection_Icon",
                table: "CandidateProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LanguagesSection_Title",
                table: "CandidateProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProjectsSection_Icon",
                table: "CandidateProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProjectsSection_Title",
                table: "CandidateProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SkillsSection_Icon",
                table: "CandidateProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SkillsSection_Title",
                table: "CandidateProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SummarySection_Icon",
                table: "CandidateProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SummarySection_Title",
                table: "CandidateProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EducationSection_Icon",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "EducationSection_Title",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "ExperienceSection_Icon",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "ExperienceSection_Title",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "InterestsSection_Icon",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "InterestsSection_Title",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "LanguagesSection_Icon",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "LanguagesSection_Title",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "ProjectsSection_Icon",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "ProjectsSection_Title",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "SkillsSection_Icon",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "SkillsSection_Title",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "SummarySection_Icon",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "SummarySection_Title",
                table: "CandidateProfiles");
        }
    }
}
