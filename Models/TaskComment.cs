using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OfficeNexus.Data;

namespace OfficeNexus.Models
{
    public class TaskComment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        public DateTime PostedAt { get; set; } = DateTime.Now;

        // Link to Task
        [Required]
        public int TaskItemId { get; set; }
        
        [ForeignKey("TaskItemId")]
        public virtual TaskItem TaskItem { get; set; } = null!;

        // Link to User (Who wrote it?)
        [Required]
        public int UserId { get; set; }
        
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
