using System.ComponentModel.DataAnnotations;

namespace OfisYonetimSistemi.Models
{
    public class ProjectImage
    {
        [Key]
        public int Id { get; set; }

        public int ProjectId { get; set; }

        public Project? Project { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ContentType { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public bool IsCover { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
