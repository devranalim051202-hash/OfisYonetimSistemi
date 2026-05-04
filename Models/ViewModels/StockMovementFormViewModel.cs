using System.ComponentModel.DataAnnotations;

namespace OfisYonetimSistemi.Models.ViewModels
{
    public class StockMovementFormViewModel
    {
        public int MaterialId { get; set; }

        [Range(0.01, 1000000)]
        public decimal Quantity { get; set; }

        [Range(0, 100000000)]
        public decimal UnitPrice { get; set; }

        [DataType(DataType.Date)]
        public DateTime MovementDate { get; set; } = DateTime.Today;

        public int? ProjectId { get; set; }

        [StringLength(100)]
        public string? DocumentNumber { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }
    }
}
