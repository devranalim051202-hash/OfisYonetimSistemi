using OfisYonetimSistemi.Models;

namespace OfisYonetimSistemi.Services;

public interface IProjectImageService
{
    Task AddImagesAsync(int projectId, IEnumerable<IFormFile> files, bool makeFirstImageCover);
    Task SetCoverImageAsync(int projectId, int? imageId);
    Task DeleteImageAsync(int imageId);
    Task DeleteImagesForProjectAsync(int projectId);
}
