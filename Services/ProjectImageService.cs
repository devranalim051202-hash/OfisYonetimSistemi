using OfisYonetimSistemi.Models;

namespace OfisYonetimSistemi.Services;

public class ProjectImageService : IProjectImageService
{
    private readonly IProjectImageRepository _repository;
    private readonly IProjectFileStorageService _fileStorageService;
    private readonly ILogger<ProjectImageService> _logger;

    public ProjectImageService(
        IProjectImageRepository repository,
        IProjectFileStorageService fileStorageService,
        ILogger<ProjectImageService> logger)
    {
        _repository = repository;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    public async Task AddImagesAsync(int projectId, IEnumerable<IFormFile>? files, bool makeFirstImageCover)
    {
        var uploadFiles = files?.Where(f => f is { Length: > 0 }).ToList() ?? new List<IFormFile>();

        if (!uploadFiles.Any())
        {
            return;
        }

        var existingImages = await _repository.GetByProjectIdAsync(projectId);
        var shouldSetCover = makeFirstImageCover || !existingImages.Any(i => i.IsCover);
        var createdImages = new List<ProjectImage>();

        try
        {
            foreach (var file in uploadFiles)
            {
                var uploadResult = await _fileStorageService.SaveProjectImageAsync(file);

                createdImages.Add(new ProjectImage
                {
                    ProjectId = projectId,
                    FileName = uploadResult.FileName,
                    OriginalFileName = Path.GetFileName(file.FileName),
                    ContentType = file.ContentType,
                    FilePath = uploadResult.FilePath,
                    FileSize = file.Length,
                    CreatedAt = DateTime.Now
                });
            }

            if (shouldSetCover && createdImages.Any())
            {
                await _repository.ClearCoverAsync(projectId);
                createdImages[0].IsCover = true;
            }

            await _repository.AddRangeAsync(createdImages);
            await _repository.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Project images could not be uploaded for project {ProjectId}.", projectId);

            foreach (var image in createdImages)
            {
                _fileStorageService.DeleteProjectImage(image.FilePath);
            }

            throw;
        }
    }

    public async Task SetCoverImageAsync(int projectId, int? imageId)
    {
        if (!imageId.HasValue)
        {
            await _repository.ClearCoverAsync(projectId);
            await _repository.SaveChangesAsync();
            return;
        }

        var image = await _repository.GetByIdAsync(imageId.Value);

        if (image == null || image.ProjectId != projectId)
        {
            throw new InvalidOperationException("Kapak gorseli bu projeye ait degil.");
        }

        await _repository.ClearCoverAsync(projectId);
        image.IsCover = true;
        await _repository.SaveChangesAsync();
    }

    public async Task DeleteImageAsync(int imageId)
    {
        var image = await _repository.GetByIdAsync(imageId);

        if (image == null)
        {
            throw new InvalidOperationException("Silinecek proje gorseli bulunamadi.");
        }

        var projectId = image.ProjectId;
        var wasCover = image.IsCover;

        _repository.Remove(image);
        await _repository.SaveChangesAsync();
        _fileStorageService.DeleteProjectImage(image.FilePath);

        if (wasCover)
        {
            var remainingImages = await _repository.GetByProjectIdAsync(projectId);
            var nextCover = remainingImages.FirstOrDefault();

            if (nextCover != null)
            {
                nextCover.IsCover = true;
                await _repository.SaveChangesAsync();
            }
        }
    }

    public async Task DeleteImagesForProjectAsync(int projectId)
    {
        var images = await _repository.GetByProjectIdAsync(projectId);

        foreach (var image in images)
        {
            _repository.Remove(image);
        }

        await _repository.SaveChangesAsync();

        foreach (var image in images)
        {
            try
            {
                _fileStorageService.DeleteProjectImage(image.FilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Project image file could not be deleted: {FilePath}", image.FilePath);
            }
        }
    }
}
