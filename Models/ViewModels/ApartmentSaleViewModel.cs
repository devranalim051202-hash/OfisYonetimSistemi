using System.ComponentModel.DataAnnotations;

namespace OfisYonetimSistemi.Models.ViewModels;

public class ApartmentSaleViewModel
{
    public int ApartmentId { get; set; }

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

    [Range(0, 999999999)]
    public decimal SalePrice { get; set; }

    [Required]
    [StringLength(100)]
    public string PaymentType { get; set; } = string.Empty;
}
