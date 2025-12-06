using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebCV.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDefaultModelToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update existing string values to '0' (int for Gpt4o) so conversion works
            migrationBuilder.Sql("UPDATE UserSettings SET DefaultModel = '0'");

            migrationBuilder.AlterColumn<int>(
                name: "DefaultModel",
                table: "UserSettings",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DefaultModel",
                table: "UserSettings",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
