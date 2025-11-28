using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OfficeNexus.Data;

namespace OfficeNexus.Models
{
    public enum LeaveType
    {
        Sick,
        Casual,
        Emergency
    }

    public enum LeaveStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public class LeaveRequest
    {
        [Key]
        public int Id { get; set; }

        // EmployeeId is set by controller, not by form - no [Required] needed here
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public virtual User Employee { get; set; } = null!;

        [Required(ErrorMessage = "Reason is required")]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        [Required(ErrorMessage = "Start date is required")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required]
        public LeaveType Type { get; set; } = LeaveType.Casual;

        public LeaveStatus Status { get; set; } = LeaveStatus.Pending;

        // CRITICAL: Separate flag for unpaid leave
        public bool IsUnpaid { get; set; } = false;

        [StringLength(500)]
        public string? AdminRemarks { get; set; }

        public DateTime RequestedOn { get; set; } = DateTime.Now;
    }
}
