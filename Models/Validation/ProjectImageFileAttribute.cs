using System.ComponentModel.DataAnnotations;

namespace OfisYonetimSistemi.Models.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public class ProjectImageFileAttribute : ValidationAttribute
{
    private const long MaxFileSize = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        if (value is IFormFile file)
        {
            return ValidateFile(file);
        }

        if (value is IEnumerable<IFormFile> files)
        {
            foreach (var currentFile in files.Where(f => f is not null))
            {
                var result = ValidateFile(currentFile);

                if (result != ValidationResult.Success)
                {
                    return result;
                }
            }

            return ValidationResult.Success;
        }

        return new ValidationResult("Gecerli bir gorsel dosyasi secin.");
    }

    private static ValidationResult? ValidateFile(IFormFile file)
    {
        if (file.Length <= 0)
        {
            return new ValidationResult("Bos dosya yuklenemez.");
        }

        if (file.Length > MaxFileSize)
        {
            return new ValidationResult("Gorsel dosyasi en fazla 5 MB olabilir.");
        }

        var extension = Path.GetExtension(file.FileName);

        if (!AllowedExtensions.Contains(extension))
        {
            return new ValidationResult("Sadece JPG, PNG veya WEBP gorsel dosyasi yuklenebilir.");
        }

        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            return new ValidationResult("Dosya tipi JPG, PNG veya WEBP olmalidir.");
        }

        return ValidationResult.Success;
    }
}
