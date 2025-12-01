using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OfficeNexus.Data;

namespace OfficeNexus.Models
{
    /// <summary>
    /// Bank account details for employee payroll processing
    /// </summary>
    public class UserBankAccount
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required(ErrorMessage = "Bank name is required")]
        [StringLength(100, ErrorMessage = "Bank name cannot exceed 100 characters")]
        [Display(Name = "Bank Name")]
        public string BankName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Account title is required")]
        // Note: StringLength removed - encrypted data will be Base64 encoded and longer than plain text
        // Validation is done on plain text before encryption, but database column must accommodate encrypted length
        [Display(Name = "Account Title")]
        public string AccountTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "IBAN is required")]
        // Note: StringLength removed - encrypted data will be Base64 encoded (~44-48 chars for 24-char IBAN)
        // Validation is done on plain text before encryption, but database column must accommodate encrypted length
        [Display(Name = "IBAN")]
        public string IBAN { get; set; } = string.Empty;

        // Note: StringLength removed - encrypted data will be Base64 encoded and longer than plain text
        // Validation is done on plain text before encryption, but database column must accommodate encrypted length
        [RegularExpression(@"^\d{14,20}$", ErrorMessage = "Account number must contain only digits (14-20 digits)")]
        [Display(Name = "Account Number")]
        public string? AccountNumber { get; set; }

        [StringLength(4, MinimumLength = 4, ErrorMessage = "Branch code must be exactly 4 digits")]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "Branch code must contain exactly 4 digits")]
        [Display(Name = "Branch Code")]
        public string? BranchCode { get; set; }

        [Required(ErrorMessage = "CNIC is required")]
        // Note: StringLength removed - encrypted data will be Base64 encoded (~24-28 chars for 15-char CNIC)
        // Validation is done on plain text before encryption, but database column must accommodate encrypted length
        [RegularExpression(@"^\d{5}-\d{7}-\d{1}$", ErrorMessage = "CNIC must be in format XXXXX-XXXXXXX-X (e.g., 35202-1234567-1)")]
        [Display(Name = "CNIC")]
        public string CNIC { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}

