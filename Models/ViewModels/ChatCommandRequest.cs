using System.ComponentModel.DataAnnotations;

namespace OfisYonetimSistemi.Models.ViewModels
{
    public class ChatCommandRequest
    {
        [Required]
        [StringLength(1000)]
        public string CommandText { get; set; } = string.Empty;
    }
}
