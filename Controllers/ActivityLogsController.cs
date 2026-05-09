using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfisYonetimSistemi.Models;
using OfisYonetimSistemi.Services;

namespace OfisYonetimSistemi.Controllers;

public class ActivityLogsController : Controller
{
    private readonly AppDbContext _context;
    private readonly ActivityLogService _activityLogService;

    public ActivityLogsController(AppDbContext context, ActivityLogService activityLogService)
    {
        _context = context;
        _activityLogService = activityLogService;
    }

    public async Task<IActionResult> Index()
    {
        if (!IsManager())
        {
            await _activityLogService.LogAsync("YetkisizDeneme", "ActivityLogs", null, "Activity log sayfasina yetkisiz erisim denendi.", false);
            return RedirectToAction("Login", "Account");
        }

        var logs = await _context.ActivityLogs
            .AsNoTracking()
            .OrderByDescending(l => l.CreatedAt)
            .Take(200)
            .ToListAsync();

        return View(logs);
    }

    private bool IsManager()
    {
        var roleName = HttpContext.Session.GetString("RoleName");
        return roleName == "Admin" || roleName == "Mudur";
    }
}
