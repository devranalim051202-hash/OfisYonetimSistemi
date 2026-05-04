using System.ComponentModel.DataAnnotations;

namespace OfisYonetimSistemi.Models.ViewModels
{
    public class VerifyEmailViewModel
    {
        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Code { get; set; } = string.Empty;
    }
}
