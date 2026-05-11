namespace OfisYonetimSistemi.Services;

public interface IProjectFileStorageService
{
    Task<(string FileName, string FilePath)> SaveProjectImageAsync(IFormFile file);
    void DeleteProjectImage(string? filePath);
}
