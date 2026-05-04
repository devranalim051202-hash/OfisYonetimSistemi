using System.ComponentModel.DataAnnotations;

namespace OfisYonetimSistemi.Models
{
    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Description { get; set; }

        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
