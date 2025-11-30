using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OfficeNexus.Migrations
{
    /// <inheritdoc />
    public partial class SecurityUpgrades_HashSecurityCodeAndRateLimiting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SecurityCode",
                table: "Users",
                newName: "SecurityCodeHash");

            migrationBuilder.CreateTable(
                name: "LoginAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    AttemptTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WasSuccessful = table.Column<bool>(type: "INTEGER", nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", nullable: true),
                    FailureReason = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginAttempts", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoginAttempts");

            migrationBuilder.RenameColumn(
                name: "SecurityCodeHash",
                table: "Users",
                newName: "SecurityCode");
        }
    }
}
