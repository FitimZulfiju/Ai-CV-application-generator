using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCV.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class MapLegacyTemplateData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE GeneratedApplications SET Template = 'Professional' WHERE Template = '0';");
            migrationBuilder.Sql("UPDATE GeneratedApplications SET Template = 'Modern' WHERE Template = '1';");
            migrationBuilder.Sql("UPDATE GeneratedApplications SET Template = 'Minimalist' WHERE Template = '2';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE GeneratedApplications SET Template = '0' WHERE Template = 'Professional';");
            migrationBuilder.Sql("UPDATE GeneratedApplications SET Template = '1' WHERE Template = 'Modern';");
            migrationBuilder.Sql("UPDATE GeneratedApplications SET Template = '2' WHERE Template = 'Minimalist';");
        }
    }
}
