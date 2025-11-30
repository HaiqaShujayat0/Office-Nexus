using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OfficeNexus.Data;

namespace OfficeNexus.Models
{
    public class TaskSubmission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        public string FileName { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        // Link to Task
        [Required]
        public int TaskItemId { get; set; }
        
        [ForeignKey("TaskItemId")]
        public virtual TaskItem TaskItem { get; set; } = null!;

        // Link to User (Employee who submitted)
        [Required]
        public int UserId { get; set; }
        
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
