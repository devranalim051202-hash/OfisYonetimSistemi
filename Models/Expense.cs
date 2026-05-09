using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace OfisYonetimSistemi.Models
{
    public class Expense
    {
        [Key]
        public int Id { get; set; }

        public int? ProjectId { get; set; }

        public Project? Project { get; set; }

        public int? CompanyId { get; set; }

        public Company? Company { get; set; }

        [Required]
        public int UserId { get; set; }

        public User? User { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string SupplierName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ExpenseType { get; set; } = "Malzeme";

        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 1000000)]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 100000000)]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 100000000)]
        public decimal Amount { get; set; }

        [DataType(DataType.Date)]
        [Required]
        public DateTime ExpenseDate { get; set; } = DateTime.Today;

        [Required]
        [StringLength(50)]
        public string PaymentStatus { get; set; } = "Odendi";

        [StringLength(100)]
        public string? DocumentNumber { get; set; }

        [Required]
        [StringLength(250)]
        public string DocumentFilePath { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string DocumentOriginalFileName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? DocumentContentType { get; set; }

        [NotMapped]
        public IFormFile? DocumentFile { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
