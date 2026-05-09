namespace OfisYonetimSistemi.Services;

public class ProjectFileStorageService : IProjectFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ProjectFileStorageService> _logger;

    public ProjectFileStorageService(IWebHostEnvironment environment, ILogger<ProjectFileStorageService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<(string FileName, string FilePath)> SaveProjectImageAsync(IFormFile file)
    {
        var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "project-images");
        Directory.CreateDirectory(uploadsPath);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(uploadsPath, fileName);

        await using var stream = File.Create(physicalPath);
        await file.CopyToAsync(stream);

        return (fileName, $"/uploads/project-images/{fileName}");
    }

    public void DeleteProjectImage(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        var trimmedPath = filePath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
        var physicalPath = Path.Combine(_environment.WebRootPath, trimmedPath);
        var uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads", "project-images");

        if (!Path.GetFullPath(physicalPath).StartsWith(Path.GetFullPath(uploadsRoot), StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Project image delete skipped for unsafe path: {FilePath}", filePath);
            return;
        }

        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }
    }
}
