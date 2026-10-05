using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCV.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddAppliedDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AppliedDate",
                table: "GeneratedApplications",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppliedDate",
                table: "GeneratedApplications");
        }
    }
}
