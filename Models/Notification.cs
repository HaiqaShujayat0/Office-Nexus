using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OfficeNexus.Data;

namespace OfficeNexus.Models
{
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Alert
    }

    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RecipientUserId { get; set; }

        [ForeignKey("RecipientUserId")]
        public virtual User RecipientUser { get; set; } = null!;

        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Link { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        public NotificationType Type { get; set; } = NotificationType.Info;
    }
}
