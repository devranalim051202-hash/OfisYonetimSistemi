using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OfisYonetimSistemi.Models
{
    public class Invoice
    {
        [Key]
        public int Id { get; set; }

        public int? ProjectId { get; set; }

        public Project? Project { get; set; }

        public int? CompanyId { get; set; }

        public Company? Company { get; set; }

        [Required]
        [StringLength(30)]
        public string DocumentType { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string DocumentNumber { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime DocumentDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(250)]
        public string? FilePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
