using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OfisYonetimSistemi.Models
{
    public class ApartmentSale
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ApartmentId { get; set; }

        public Apartment? Apartment { get; set; }

        [Required]
        [StringLength(150)]
        public string BuyerFullName { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string BuyerIdentityNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string BuyerPhone { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string BuyerAddress { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalePrice { get; set; }

        [Required]
        [StringLength(100)]
        public string PaymentType { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime SaleDate { get; set; } = DateTime.Today;

        [Required]
        public string ContractText { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
