using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using OfficeNexus.Models;

namespace OfficeNexus.Data
{
    // --- Domain Models ---
    public enum UserRole
    {
        Admin,
        Employee
    }

    public enum EmployeeStatus
    {
        Active = 1,
        Pending = 2,
        Terminated = 3
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
        
        // Security Code Hash - Only for Admin users (BCrypt hashed, nullable for employees)
        public string? SecurityCodeHash { get; set; }
        
        // --- Contact Information ---
        
        [Required]
        [Phone]
        [StringLength(20, MinimumLength = 10, ErrorMessage = "Phone number must be between 10 and 20 characters")]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string HomeAddress { get; set; } = string.Empty;
        
        // Employee Status - Active, Pending, or Terminated
        public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
        
        // --- Profile Management ---
        
        // Profile picture path (e.g., /images/profiles/abc123.jpg)
        public string? ProfilePicturePath { get; set; }
        
        // Email verification fields for secure email updates
        public string? NewEmailCandidate { get; set; }
        public string? EmailVerificationToken { get; set; }
        
        // Security Stamp - Changes when password/email changes to invalidate all sessions
        public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();
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

    // --- Login Attempt Tracking (Rate Limiting & Security Audit) ---
    public class LoginAttempt
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Email { get; set; } = string.Empty;
        
        public DateTime AttemptTime { get; set; } = DateTime.Now;
        
        public bool WasSuccessful { get; set; }
        
        public string? IpAddress { get; set; }
        
        public string? FailureReason { get; set; }  // "InvalidPassword", "InvalidSecurityCode", "AccountLocked"
    }

    // --- Database Context ---
    public class OfficeDbContext : DbContext
    {
        public OfficeDbContext(DbContextOptions<OfficeDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<VisitorLog> VisitorLogs { get; set; }
        public DbSet<TaskItem> TaskItems { get; set; }
        public DbSet<TaskComment> TaskComments { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<TaskSubmission> TaskSubmissions { get; set; }
        public DbSet<TaskAssignment> TaskAssignments { get; set; }
        public DbSet<LoginAttempt> LoginAttempts { get; set; }

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
