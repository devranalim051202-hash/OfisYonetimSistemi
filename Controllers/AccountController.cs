using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfisYonetimSistemi.Models;
using OfisYonetimSistemi.Models.ViewModels;
using OfisYonetimSistemi.Services;
using System.Text.Json;

namespace OfisYonetimSistemi.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _context;
    private readonly ActivityLogService _activityLogService;

    public AccountController(AppDbContext context, ActivityLogService activityLogService)
    {
        _context = context;
        _activityLogService = activityLogService;
    }

    public IActionResult Login()
    {
        return View();
    }

    public IActionResult AdminLogin()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminLogin(string email, string password)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email && u.Password == password);

        if (user?.Role?.Name == "Admin" || user?.Role?.Name == "Mudur")
        {
            SetSession(user);
            await _activityLogService.LogLoginAsync(user, "Giris", "Admin/Mudur girisi yapildi.", true);
            return RedirectToAction("Index", "Manager");
        }

        await _activityLogService.LogLoginAsync(user, "GirisDenemesi", $"Basarisiz admin/mudur girisi: {email}", false);
        ViewBag.Message = "Admin veya mudur hesabi bulunamadi.";
        return View();
    }

    public IActionResult PersonnelLogin()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PersonnelLogin(string email, string password)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email && u.Password == password);

        if (user?.Role?.Name != null && user.Role.Name != "Admin" && user.Role.Name != "Mudur")
        {
            SetSession(user);
            await _activityLogService.LogLoginAsync(user, "Giris", "Personel girisi yapildi.", true);
            return RedirectToAction("Index", "Personnel");
        }

        await _activityLogService.LogLoginAsync(user, "GirisDenemesi", $"Basarisiz personel girisi: {email}", false);
        ViewBag.Message = "Calisan hesabi bulunamadi.";
        return View();
    }

    public async Task<IActionResult> Logout()
    {
        await _activityLogService.LogAsync("Cikis", "Oturum", HttpContext.Session.GetInt32("UserId"), "Kullanici sistemden cikis yapti.");
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (await _context.Users.AnyAsync(u => u.Email == model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "Bu mail adresi zaten kullaniliyor.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var verificationCode = Random.Shared.Next(100000, 999999).ToString();

        HttpContext.Session.SetString("PendingRegister", JsonSerializer.Serialize(model));
        HttpContext.Session.SetString("VerificationCode", verificationCode);

        TempData["VerificationInfo"] = $"Dogrulama kodu mail adresine gonderildi. Gelistirme kodu: {verificationCode}";
        return RedirectToAction(nameof(VerifyEmail));
    }

    public IActionResult VerifyEmail()
    {
        if (HttpContext.Session.GetString("PendingRegister") == null)
        {
            return RedirectToAction(nameof(Register));
        }

        return View(new VerifyEmailViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
    {
        var pendingJson = HttpContext.Session.GetString("PendingRegister");
        var verificationCode = HttpContext.Session.GetString("VerificationCode");

        if (pendingJson == null || verificationCode == null)
        {
            return RedirectToAction(nameof(Register));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.Code != verificationCode)
        {
            ModelState.AddModelError(nameof(model.Code), "Dogrulama kodu hatali.");
            return View(model);
        }

        var registerModel = JsonSerializer.Deserialize<RegisterViewModel>(pendingJson);

        if (registerModel == null)
        {
            return RedirectToAction(nameof(Register));
        }

        var user = new User
        {
            FirstName = registerModel.FirstName,
            LastName = registerModel.LastName,
            FullName = $"{registerModel.FirstName} {registerModel.LastName}",
            PhoneNumber = registerModel.PhoneNumber,
            CompanyName = registerModel.CompanyName,
            CompanySize = registerModel.CompanySize,
            Email = registerModel.Email,
            Password = registerModel.Password,
            RoleId = 5
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        HttpContext.Session.Remove("PendingRegister");
        HttpContext.Session.Remove("VerificationCode");

        TempData["SuccessMessage"] = "Uyelik olusturuldu. Mudur girisi yapabilirsiniz.";
        return RedirectToAction(nameof(AdminLogin));
    }

    private void SetSession(User user)
    {
        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("FullName", user.FullName);
        HttpContext.Session.SetString("RoleName", user.Role?.Name ?? string.Empty);
        HttpContext.Session.SetString("CompanyName", user.CompanyName);
        HttpContext.Session.SetInt32("CompanySize", user.CompanySize);
    }
}
