using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OfisYonetimSistemi.Models
{
    public class Apartment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        public Project? Project { get; set; }

        public int FloorNumber { get; set; }

        [Required]
        [StringLength(30)]
        public string ApartmentNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string RoomType { get; set; } = "1+1";

        [Column(TypeName = "decimal(18,2)")]
        public decimal GrossArea { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetArea { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public bool IsSold { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ApartmentSale? Sale { get; set; }
    }
}
