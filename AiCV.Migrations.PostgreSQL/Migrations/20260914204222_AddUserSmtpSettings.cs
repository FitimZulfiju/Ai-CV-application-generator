using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AiCV.Migrations.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSmtpSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserSmtpSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    SmtpHost = table.Column<string>(type: "text", nullable: true),
                    SmtpPort = table.Column<int>(type: "integer", nullable: false),
                    SmtpUser = table.Column<string>(type: "text", nullable: true),
                    SmtpPassword = table.Column<string>(type: "text", nullable: true),
                    EnableSsl = table.Column<bool>(type: "boolean", nullable: false),
                    FromEmail = table.Column<string>(type: "text", nullable: true),
                    FromName = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSmtpSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSmtpSettings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSmtpSettings_UserId",
                table: "UserSmtpSettings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSmtpSettings");
        }
    }
}
