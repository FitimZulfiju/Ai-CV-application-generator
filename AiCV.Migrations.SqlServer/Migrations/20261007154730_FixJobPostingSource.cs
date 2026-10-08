using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCV.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class FixJobPostingSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE [JobPostings]
                SET [Source] = [CompanyName],
                    [CompanyName] = ''
                WHERE ([Source] IS NULL OR [Source] = '')
                AND LOWER([CompanyName]) IN ('jobindex', 'linkedin', 'indeed', 'glassdoor', 'the hub', 'ofir', 'jobnet');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
