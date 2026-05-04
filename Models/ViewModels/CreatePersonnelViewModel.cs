using System.ComponentModel.DataAnnotations;

namespace OfisYonetimSistemi.Models.ViewModels
{
    public class CreatePersonnelViewModel
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public int RoleId { get; set; } = 4;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;
    }
}
