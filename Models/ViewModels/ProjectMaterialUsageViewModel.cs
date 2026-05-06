using System.ComponentModel.DataAnnotations;

namespace OfisYonetimSistemi.Models.ViewModels
{
    public class ProjectMaterialUsageViewModel
    {
        public int ProjectId { get; set; }

        [Range(1, int.MaxValue)]
        public int MaterialId { get; set; }

        public int? CompanyId { get; set; }

        [Range(0.01, 1000000)]
        public decimal Quantity { get; set; }

        [Range(0, 100000000)]
        public decimal UnitPrice { get; set; }

        public DateTime MovementDate { get; set; } = DateTime.Today;
    }
}
