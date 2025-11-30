using System.ComponentModel.DataAnnotations;

namespace OfficeNexus.Models
{
    public enum TaskStatus
    {
        ToDo = 1,
        InProgress = 2,
        InReview = 3,
        Completed = 4
    }

    public class TaskAssignment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public int AssignedToUserId { get; set; }

        public int AssignedByUserId { get; set; }

        public TaskStatus Status { get; set; } = TaskStatus.ToDo;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? DueDate { get; set; }

        public DateTime? CompletedAt { get; set; }
    }
}
