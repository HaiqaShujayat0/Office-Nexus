using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OfficeNexus.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskHistoryAndReviewTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "TaskItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "TaskItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewedByAdminId",
                table: "TaskItems",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "ReviewedByAdminId",
                table: "TaskItems");
        }
    }
}
