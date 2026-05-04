using System.ComponentModel.DataAnnotations;

namespace OfisYonetimSistemi.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string CompanyName { get; set; } = string.Empty;

        [Range(1, 100000)]
        public int CompanySize { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;
    }
}
