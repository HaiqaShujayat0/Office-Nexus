using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OfficeNexus.Data;

namespace OfficeNexus.Models
{
    public enum TaskWorkflowStatus
    {
        ToDo,
        InProgress,
        InReview,
        Done
    }

    public enum TaskPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    public class TaskItem
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Task title is required")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        // Tier 1: Workflow & Priority
        public TaskWorkflowStatus Status { get; set; } = TaskWorkflowStatus.ToDo;
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        // Tier 3: Attachments (Stores the file path, e.g., "/uploads/doc.pdf")
        public string? AttachmentPath { get; set; }
        public string? AttachmentFileName { get; set; } // Original filename for display

        [Required]
        public DateTime DueDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
        public DateTime? SubmittedAt { get; set; } // When employee submitted for review
        
        // Admin Review Tracking
        public DateTime? ReviewedAt { get; set; } // When admin marked as done
        public int? ReviewedByAdminId { get; set; } // Which admin reviewed
        public bool IsArchived { get; set; } = false; // For history/archive functionality

        // Foreign Keys
        [Required]
        public int AssignedToUserId { get; set; }
        
        [ForeignKey("AssignedToUserId")]
        public virtual User AssignedToUser { get; set; } = null!;

        [Required]
        public int CreatedByAdminId { get; set; }
        
        [ForeignKey("CreatedByAdminId")]
        public virtual User CreatedByAdmin { get; set; } = null!;

        // Tier 2: Relationship to Comments
        public virtual ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();

        // Tier 3: Submissions (Multiple files)
        public virtual ICollection<TaskSubmission> Submissions { get; set; } = new List<TaskSubmission>();
    }
}
