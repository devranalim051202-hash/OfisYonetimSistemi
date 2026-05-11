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

        [Range(0, 200)]
        public int FloorCount { get; set; }

        [Range(0, 5000)]
        public int ApartmentCount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

        public ICollection<Expense> Expenses { get; set; } = new List<Expense>();

        public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

        public ICollection<Apartment> Apartments { get; set; } = new List<Apartment>();

        public ICollection<ProjectImage> ProjectImages { get; set; } = new List<ProjectImage>();
    }
}
