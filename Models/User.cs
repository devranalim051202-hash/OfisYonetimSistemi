using System.ComponentModel.DataAnnotations;

namespace OfisYonetimSistemi.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string CompanyName { get; set; } = string.Empty;

        public int CompanySize { get; set; }

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public int RoleId { get; set; } = 4;

        public Role? Role { get; set; }

        public ICollection<Expense> Expenses { get; set; } = new List<Expense>();

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
