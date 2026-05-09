using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfisYonetimSistemi.Models;
using OfisYonetimSistemi.Models.ViewModels;
using OfisYonetimSistemi.Services;

namespace OfisYonetimSistemi.Controllers;

public class ManagerController : Controller
{
    private readonly AppDbContext _context;
    private readonly ActivityLogService _activityLogService;

    public ManagerController(AppDbContext context, ActivityLogService activityLogService)
    {
        _context = context;
        _activityLogService = activityLogService;
    }

    public async Task<IActionResult> Index()
    {
        if (!IsManager())
        {
            return RedirectToAction("Login", "Account");
        }

        ViewBag.PersonnelCount = await _context.Users.CountAsync(u => u.RoleId != 1);
        ViewBag.ProjectCount = await _context.Projects.CountAsync();
        ViewBag.InvoiceCount = await _context.Invoices.CountAsync();
        ViewBag.ExpenseCount = await _context.Expenses.CountAsync();
        ViewBag.RecentActivityLogs = await _context.ActivityLogs
            .OrderByDescending(l => l.CreatedAt)
            .Take(8)
            .ToListAsync();

        var users = await _context.Users
            .Include(u => u.Role)
            .Where(u => u.RoleId != 1)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        return View(users);
    }

    public async Task<IActionResult> CreatePersonnel()
    {
        if (!IsManager())
        {
            await _activityLogService.LogAsync("YetkisizDeneme", "Personeller", null, "Personel olusturma sayfasina yetkisiz erisim denendi.", false);
            return RedirectToAction("Login", "Account");
        }

        await FillRolesAsync();
        return View(new CreatePersonnelViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePersonnel(CreatePersonnelViewModel model)
    {
        if (!IsManager())
        {
            await _activityLogService.LogAsync("YetkisizDeneme", "Personeller", null, "Personel olusturma islemi yetkisiz denendi.", false);
            return RedirectToAction("Login", "Account");
        }

        if (await _context.Users.AnyAsync(u => u.Email == model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "Bu mail adresi zaten kullaniliyor.");
        }

        if (!ModelState.IsValid)
        {
            await FillRolesAsync();
            return View(model);
        }

        var user = new User
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            FullName = $"{model.FirstName} {model.LastName}",
            PhoneNumber = string.Empty,
            CompanyName = HttpContext.Session.GetString("CompanyName") ?? string.Empty,
            CompanySize = HttpContext.Session.GetInt32("CompanySize") ?? 0,
            Email = model.Email,
            RoleId = model.RoleId,
            Password = model.Password
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync("Ekleme", "Personeller", user.Id, $"{user.FullName} personel hesabi olusturuldu.");

        TempData["SuccessMessage"] = "Personel hesabi olusturuldu.";
        return RedirectToAction(nameof(Index));
    }

    private bool IsManager()
    {
        var roleName = HttpContext.Session.GetString("RoleName");
        return roleName == "Admin" || roleName == "Mudur";
    }

    private async Task FillRolesAsync()
    {
        var roles = await _context.Roles
            .Where(r => r.Name != "Admin" && r.Name != "Mudur")
            .OrderBy(r => r.Name)
            .ToListAsync();

        ViewBag.Roles = new SelectList(roles, "Id", "Name");
    }
}
