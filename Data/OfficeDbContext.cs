using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace OfficeNexus.Data
{
    // --- Domain Models ---
    public enum UserRole
    {
        Admin,
        Employee
    }

    public class User
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty; // Storing Hashed Passwords

        public UserRole Role { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // --- Enhanced Fields for Office Automation ---
        
        [Required]
        public string JobTitle { get; set; } = "Employee";
        
        public string Department { get; set; } = "General";
        
        // For Payroll calculation
        public decimal BasicSalary { get; set; } = 0;
        
        // Security Code - Only for Admin users (nullable for employees)
        public string? SecurityCode { get; set; }
    }

    public class VisitorLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string VisitorName { get; set; } = string.Empty;

        [Required]
        public string Purpose { get; set; } = string.Empty;

        public DateTime TimeIn { get; set; } = DateTime.Now;
        public DateTime? TimeOut { get; set; }

        // Logic: "Internal" means from another branch, "Outsider" is a guest
        public string VisitorType { get; set; } = "Outsider"; 

        // Which employee handled this visitor
        public int EmployeeId { get; set; }
        public User? Employee { get; set; }
    }

    // --- Database Context ---
    public class OfficeDbContext : DbContext
    {
        public OfficeDbContext(DbContextOptions<OfficeDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<VisitorLog> VisitorLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Seed default Admin if needed is usually done in Program.cs, 
            // but we ensure unique emails here.
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}