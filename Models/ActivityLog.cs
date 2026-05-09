using System.ComponentModel.DataAnnotations;

namespace OfisYonetimSistemi.Models
{
    public class ActivityLog
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; }

        public User? User { get; set; }

        [Required]
        [StringLength(150)]
        public string UserFullName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string UserRole { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ModuleName { get; set; } = string.Empty;

        public int? RecordId { get; set; }

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        public bool IsSuccessful { get; set; }

        [StringLength(100)]
        public string? IpAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
