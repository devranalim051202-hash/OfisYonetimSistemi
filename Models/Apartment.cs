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

        [Range(0, 200, ErrorMessage = "Kat numarasi 0 ile 200 arasinda olmalidir.")]
        public int FloorNumber { get; set; }

        [Required]
        [StringLength(30, ErrorMessage = "Daire numarasi en fazla 30 karakter olabilir.")]
        public string ApartmentNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(4, ErrorMessage = "Oda tipi en fazla 4 karakter olabilir.")]
        public string RoomType { get; set; } = "1+1";

        [Column(TypeName = "decimal(18,2)")]
        [Range(10, 10000, ErrorMessage = "Brut alan 10 ile 10000 m2 arasinda olmalidir.")]
        public decimal GrossArea { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(10, 10000, ErrorMessage = "Net alan 10 ile 10000 m2 arasinda olmalidir.")]
        public decimal NetArea { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 1000000000, ErrorMessage = "Fiyat 0 ile 1000000000 TL arasinda olmalidir.")]
        public decimal Price { get; set; }

        public bool IsSold { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ApartmentSale? Sale { get; set; }
    }
}
