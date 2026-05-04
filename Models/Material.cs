using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OfisYonetimSistemi.Models
{
    public class Material
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Category { get; set; }

        [Required]
        [StringLength(30)]
        public string Unit { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentStock { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MinimumStockLevel { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    }
}
