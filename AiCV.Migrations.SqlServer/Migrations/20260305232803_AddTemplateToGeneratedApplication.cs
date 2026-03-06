using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCV.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddTemplateToGeneratedApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Template",
                table: "GeneratedApplications",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.DropForeignKey(
                name: "FK_GeneratedApplications_AspNetUsers_UserId",
                table: "GeneratedApplications"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_GeneratedApplications_AspNetUsers_UserId",
                table: "GeneratedApplications",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Template", table: "GeneratedApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_GeneratedApplications_AspNetUsers_UserId",
                table: "GeneratedApplications"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_GeneratedApplications_AspNetUsers_UserId",
                table: "GeneratedApplications",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );
        }
    }
}
