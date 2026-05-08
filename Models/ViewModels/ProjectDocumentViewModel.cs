using System.ComponentModel.DataAnnotations;

namespace OfisYonetimSistemi.Models.ViewModels
{
    public class ProjectDocumentViewModel
    {
        public int ProjectId { get; set; }

        [Required]
        public IFormFile? DocumentFile { get; set; }

        [Required]
        [StringLength(100)]
        public string DocumentNumber { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

    }
}
