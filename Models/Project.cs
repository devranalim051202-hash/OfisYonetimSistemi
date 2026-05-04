using System.ComponentModel.DataAnnotations;

namespace OfisYonetimSistemi.Models
{
    public class Project
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Aktif";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

        public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    }
}
