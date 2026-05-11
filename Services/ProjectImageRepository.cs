using Microsoft.EntityFrameworkCore;
using OfisYonetimSistemi.Models;

namespace OfisYonetimSistemi.Services;

public class ProjectImageRepository : IProjectImageRepository
{
    private readonly AppDbContext _context;

    public ProjectImageRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProjectImage>> GetByProjectIdAsync(int projectId)
    {
        return await _context.ProjectImages
            .Where(i => i.ProjectId == projectId)
            .OrderByDescending(i => i.IsCover)
            .ThenBy(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<ProjectImage?> GetByIdAsync(int id)
    {
        return await _context.ProjectImages.FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task AddRangeAsync(IEnumerable<ProjectImage> images)
    {
        await _context.ProjectImages.AddRangeAsync(images);
    }

    public void Remove(ProjectImage image)
    {
        _context.ProjectImages.Remove(image);
    }

    public async Task ClearCoverAsync(int projectId)
    {
        var images = await _context.ProjectImages
            .Where(i => i.ProjectId == projectId && i.IsCover)
            .ToListAsync();

        foreach (var image in images)
        {
            image.IsCover = false;
        }
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}
