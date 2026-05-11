using System.ComponentModel.DataAnnotations;
using OfisYonetimSistemi.Models.Validation;

namespace OfisYonetimSistemi.Models.ViewModels;

public class ProjectFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(200)]
    public string? Location { get; set; }

    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Aktif";

    [Range(0, 200)]
    public int FloorCount { get; set; }

    [Range(0, 5000)]
    public int ApartmentCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ProjectImageFile]
    public List<IFormFile>? ProjectImages { get; set; }

    public bool MakeFirstImageCover { get; set; } = true;

    public int? CoverImageId { get; set; }

    public List<ProjectImage> ExistingImages { get; set; } = new();

    public static ProjectFormViewModel FromProject(Project project)
    {
        return new ProjectFormViewModel
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            Location = project.Location,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            Status = project.Status,
            FloorCount = project.FloorCount,
            ApartmentCount = project.ApartmentCount,
            CreatedAt = project.CreatedAt,
            CoverImageId = project.ProjectImages.FirstOrDefault(i => i.IsCover)?.Id,
            MakeFirstImageCover = false,
            ExistingImages = project.ProjectImages.OrderByDescending(i => i.IsCover).ThenBy(i => i.CreatedAt).ToList()
        };
    }

    public Project ToProject()
    {
        return new Project
        {
            Id = Id,
            Name = Name,
            Description = Description,
            Location = Location,
            StartDate = StartDate,
            EndDate = EndDate,
            Status = Status,
            FloorCount = FloorCount,
            ApartmentCount = ApartmentCount,
            CreatedAt = CreatedAt
        };
    }
}
