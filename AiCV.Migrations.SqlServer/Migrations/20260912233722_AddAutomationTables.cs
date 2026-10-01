using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCV.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddAutomationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing rows predate the status workflow and are treated as pending review.
            // An empty string is not a valid ApplicationStatus value.
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "GeneratedApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "PendingReview");

            migrationBuilder.CreateTable(
                name: "AutomationSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CronExpression = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxApplicationsPerRun = table.Column<int>(type: "int", nullable: false),
                    Providers = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastRunUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextRunUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutomationSettings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AutomationQueries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AutomationSettingsId = table.Column<int>(type: "int", nullable: false),
                    Query = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Region = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobAgeDays = table.Column<int>(type: "int", nullable: false),
                    MaxResults = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationQueries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutomationQueries_AutomationSettings_AutomationSettingsId",
                        column: x => x.AutomationSettingsId,
                        principalTable: "AutomationSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationQueries_AutomationSettingsId",
                table: "AutomationQueries",
                column: "AutomationSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_AutomationSettings_UserId",
                table: "AutomationSettings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutomationQueries");

            migrationBuilder.DropTable(
                name: "AutomationSettings");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "GeneratedApplications");
        }
    }
}
