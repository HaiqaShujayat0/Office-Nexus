using System.ComponentModel.DataAnnotations;

namespace OfficeNexus.ViewModels
{
    /// <summary>
    /// View model for individual employee bank info status
    /// </summary>
    public class BankStatusViewModel
    {
        [Display(Name = "Employee Name")]
        public string EmployeeName { get; set; } = string.Empty;

        [Display(Name = "Employee ID")]
        public int EmployeeId { get; set; }

        [Display(Name = "Department")]
        public string Department { get; set; } = string.Empty;

        [Display(Name = "Has Submitted Bank Info")]
        public bool HasSubmittedBankInfo { get; set; }

        [Display(Name = "Submission Date")]
        public DateTime? SubmissionDate { get; set; }
    }

    /// <summary>
    /// View model for bank compliance analytics dashboard
    /// </summary>
    public class BankAnalyticsDashboardViewModel
    {
        [Display(Name = "Total Employees")]
        public int TotalEmployees { get; set; }

        [Display(Name = "Submitted Count")]
        public int SubmittedCount { get; set; }

        [Display(Name = "Pending Count")]
        public int PendingCount { get; set; }

        [Display(Name = "Employee Statuses")]
        public List<BankStatusViewModel> EmployeeStatuses { get; set; } = new List<BankStatusViewModel>();
    }
}

