using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OfisYonetimSistemi.Models
{
    public class StockMovement
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MaterialId { get; set; }

        public Material? Material { get; set; }

        public int? ProjectId { get; set; }

        public Project? Project { get; set; }

        public int? CompanyId { get; set; }

        public Company? Company { get; set; }

        [Required]
        [StringLength(20)]
        public string MovementType { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [DataType(DataType.Date)]
        public DateTime MovementDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? DocumentNumber { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
