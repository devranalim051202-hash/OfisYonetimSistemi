using OfisYonetimSistemi.Models;

namespace OfisYonetimSistemi.Services;

public class ActivityLogService
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ActivityLogService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string action, string moduleName, int? recordId, string description, bool isSuccessful = true)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var userId = httpContext?.Session.GetInt32("UserId");
        var fullName = httpContext?.Session.GetString("FullName") ?? "Bilinmeyen Kullanici";
        var roleName = httpContext?.Session.GetString("RoleName") ?? "Bilinmeyen Rol";
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();

        _context.ActivityLogs.Add(new ActivityLog
        {
            UserId = userId,
            UserFullName = fullName,
            UserRole = roleName,
            Action = action,
            ModuleName = moduleName,
            RecordId = recordId,
            Description = description,
            IsSuccessful = isSuccessful,
            IpAddress = ipAddress,
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();
    }

    public async Task LogLoginAsync(User? user, string action, string description, bool isSuccessful)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();

        _context.ActivityLogs.Add(new ActivityLog
        {
            UserId = user?.Id,
            UserFullName = user?.FullName ?? "Bilinmeyen Kullanici",
            UserRole = user?.Role?.Name ?? "Bilinmeyen Rol",
            Action = action,
            ModuleName = "Oturum",
            RecordId = user?.Id,
            Description = description,
            IsSuccessful = isSuccessful,
            IpAddress = ipAddress,
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();
    }
}
