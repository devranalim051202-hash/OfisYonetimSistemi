using System.ComponentModel.DataAnnotations;

namespace OfisYonetimSistemi.Models
{
    public class Company
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ContactPerson { get; set; }

        [StringLength(30)]
        public string? Phone { get; set; }

        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(250)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string? TaxNumber { get; set; }

        [StringLength(100)]
        public string? TaxOffice { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

        public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    }
}
