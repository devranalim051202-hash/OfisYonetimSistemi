using OfisYonetimSistemi.Models;

namespace OfisYonetimSistemi.Services;

public interface IProjectImageRepository
{
    Task<List<ProjectImage>> GetByProjectIdAsync(int projectId);
    Task<ProjectImage?> GetByIdAsync(int id);
    Task AddRangeAsync(IEnumerable<ProjectImage> images);
    void Remove(ProjectImage image);
    Task ClearCoverAsync(int projectId);
    Task SaveChangesAsync();
}
