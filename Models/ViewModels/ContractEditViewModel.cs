using System.ComponentModel.DataAnnotations;

namespace OfisYonetimSistemi.Models.ViewModels;

public class ContractEditViewModel
{
    public int ApartmentId { get; set; }

    public int ApartmentSaleId { get; set; }

    [Required]
    public string ContractText { get; set; } = string.Empty;
}
