using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OfficeNexus.Migrations
{
    /// <inheritdoc />
    public partial class EncryptBankAccountFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQLite TEXT columns are already unlimited, but we need to ensure
            // Entity Framework metadata reflects that encrypted data can be longer.
            // The maxLength constraints in the original migration were just metadata.
            // Since SQLite doesn't enforce column length limits, we just need to ensure
            // the model doesn't have StringLength attributes (which we've already removed).
            
            // For SQLite, no actual ALTER TABLE is needed since TEXT is unlimited.
            // However, if you're using SQL Server or other databases in production,
            // you would need to alter the columns to NVARCHAR(MAX) or VARCHAR(MAX).
            
            // Example for SQL Server (commented out - adjust for your production DB):
            // migrationBuilder.AlterColumn<string>(
            //     name: "AccountTitle",
            //     table: "UserBankAccounts",
            //     type: "NVARCHAR(MAX)",
            //     nullable: false);
            // 
            // migrationBuilder.AlterColumn<string>(
            //     name: "IBAN",
            //     table: "UserBankAccounts",
            //     type: "NVARCHAR(MAX)",
            //     nullable: false);
            // 
            // migrationBuilder.AlterColumn<string>(
            //     name: "AccountNumber",
            //     table: "UserBankAccounts",
            //     type: "NVARCHAR(MAX)",
            //     nullable: true);
            // 
            // migrationBuilder.AlterColumn<string>(
            //     name: "CNIC",
            //     table: "UserBankAccounts",
            //     type: "NVARCHAR(MAX)",
            //     nullable: false);
            
            // For SQLite, we just add a comment to document the change
            // The actual columns are already TEXT (unlimited)
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No rollback needed - SQLite TEXT columns remain unlimited
        }
    }
}

