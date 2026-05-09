using System.ComponentModel.DataAnnotations;

namespace OfisYonetimSistemi.Models
{
    public class ChatBotLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        public User? User { get; set; }

        [Required]
        [StringLength(1000)]
        public string CommandText { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string DetectedAction { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string ResponseText { get; set; } = string.Empty;

        public bool IsSuccessful { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
