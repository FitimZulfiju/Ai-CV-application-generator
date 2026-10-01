using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCV.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddApplyUrlToJobPosting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplyUrl",
                table: "JobPostings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplyUrl",
                table: "JobPostings");
        }
    }
}
