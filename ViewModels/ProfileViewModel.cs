using System.ComponentModel.DataAnnotations;

namespace OfficeNexus.ViewModels
{
    /// <summary>
    /// View model for displaying user profile information
    /// </summary>
    public class ProfileViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string HomeAddress { get; set; } = string.Empty;
        public string? ProfilePicturePath { get; set; }
        public DateTime MemberSince { get; set; }
    }

    /// <summary>
    /// View model for changing password with security validation
    /// </summary>
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
        [RegularExpression(@"^(?=.*\d).+$", ErrorMessage = "Password must contain at least one digit")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your new password")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// View model for updating email with verification
    /// </summary>
    public class UpdateEmailViewModel
    {
        [Required(ErrorMessage = "New email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "New Email Address")]
        public string NewEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Current password is required for security")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// View model for updating personal information
    /// </summary>
    public class UpdateProfileViewModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(20, MinimumLength = 10, ErrorMessage = "Phone number must be between 10 and 20 characters")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Home address is required")]
        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        [Display(Name = "Home Address")]
        public string HomeAddress { get; set; } = string.Empty;
    }

    /// <summary>
    /// View model for bank account details
    /// </summary>
    public class BankDetailsViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Bank name is required")]
        [StringLength(100, ErrorMessage = "Bank name cannot exceed 100 characters")]
        [Display(Name = "Bank Name")]
        public string BankName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Account title is required")]
        [StringLength(100, ErrorMessage = "Account title cannot exceed 100 characters")]
        [Display(Name = "Account Title")]
        public string AccountTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "IBAN is required")]
        [Display(Name = "IBAN")]
        public string IBAN { get; set; } = string.Empty;

        [StringLength(20, MinimumLength = 14, ErrorMessage = "Account number must be between 14 and 20 digits")]
        [RegularExpression(@"^\d{14,20}$", ErrorMessage = "Account number must contain only digits (14-20 digits)")]
        [Display(Name = "Account Number")]
        public string? AccountNumber { get; set; }

        [StringLength(4, MinimumLength = 4, ErrorMessage = "Branch code must be exactly 4 digits")]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "Branch code must contain exactly 4 digits")]
        [Display(Name = "Branch Code")]
        public string? BranchCode { get; set; }

        [Required(ErrorMessage = "CNIC is required")]
        [RegularExpression(@"^\d{5}-\d{7}-\d{1}$", ErrorMessage = "CNIC must be in format XXXXX-XXXXXXX-X (e.g., 35202-1234567-1)")]
        [Display(Name = "CNIC")]
        public string CNIC { get; set; } = string.Empty;
    }
}
