using Microsoft.AspNetCore.RateLimiting;
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
    private readonly LoginAttemptTracker _loginAttemptTracker;

    public AccountController(
        AppDbContext context,
        ActivityLogService activityLogService,
        LoginAttemptTracker loginAttemptTracker)
    {
        _context = context;
        _activityLogService = activityLogService;
        _loginAttemptTracker = loginAttemptTracker;
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
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> AdminLogin(string email, string password)
    {
        email = NormalizeEmail(email);
        var ipAddress = ClientIpAddress();
        var lockoutStatus = _loginAttemptTracker.GetLockoutStatus(email, ipAddress);

        if (lockoutStatus.IsLocked)
        {
            ApplyMvcLockout(lockoutStatus);
            return View();
        }

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email && u.Password == password);

        if (user?.Role?.Name == "Admin" || user?.Role?.Name == "Mudur")
        {
            _loginAttemptTracker.ResetEmail(email);
            SetSession(user);
            await _activityLogService.LogLoginAsync(user, "Giris", "Admin/Mudur girisi yapildi.", true);
            return RedirectToAction("Index", "Manager");
        }

        var failedStatus = _loginAttemptTracker.RecordFailedAttempt(email, ipAddress);

        if (failedStatus.IsLocked)
        {
            ApplyMvcLockout(failedStatus);
            return View();
        }

        ViewBag.Message = "Email veya sifre hatali.";
        return View();
    }

    public IActionResult PersonnelLogin()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> PersonnelLogin(string email, string password)
    {
        email = NormalizeEmail(email);
        var ipAddress = ClientIpAddress();
        var lockoutStatus = _loginAttemptTracker.GetLockoutStatus(email, ipAddress);

        if (lockoutStatus.IsLocked)
        {
            ApplyMvcLockout(lockoutStatus);
            return View();
        }

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email && u.Password == password);

        if (user?.Role?.Name != null && user.Role.Name != "Admin" && user.Role.Name != "Mudur")
        {
            _loginAttemptTracker.ResetEmail(email);
            SetSession(user);
            await _activityLogService.LogLoginAsync(user, "Giris", "Personel girisi yapildi.", true);
            return RedirectToAction("Index", "Personnel");
        }

        var failedStatus = _loginAttemptTracker.RecordFailedAttempt(email, ipAddress);

        if (failedStatus.IsLocked)
        {
            ApplyMvcLockout(failedStatus);
            return View();
        }

        ViewBag.Message = "Email veya sifre hatali.";
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
        HttpContext.Session.Clear();
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        model.Email = NormalizeEmail(model.Email);

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
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            _context.Users.Remove(user);
            ModelState.AddModelError(nameof(RegisterViewModel.Email), "Bu mail adresi zaten kullaniliyor.");
            return View(nameof(Register), registerModel);
        }

        HttpContext.Session.Remove("PendingRegister");
        HttpContext.Session.Remove("VerificationCode");
        user.Role = await _context.Roles.FindAsync(user.RoleId);
        SetSession(user);

        TempData["SuccessMessage"] = "Uyelik olusturuldu. Yeni sirket hesabiniz acildi.";
        return RedirectToAction("Index", "Manager");
    }

    private void SetSession(User user)
    {
        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("FullName", user.FullName);
        HttpContext.Session.SetString("RoleName", user.Role?.Name ?? string.Empty);
        HttpContext.Session.SetString("CompanyName", user.CompanyName);
        HttpContext.Session.SetInt32("CompanySize", user.CompanySize);
    }

    private static string NormalizeEmail(string? email)
    {
        return (email ?? string.Empty).Trim().ToLowerInvariant();
    }

    private string ClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private void ApplyMvcLockout(LoginLockoutStatus lockoutStatus)
    {
        var retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(lockoutStatus.RetryAfter.TotalSeconds));
        Response.StatusCode = StatusCodes.Status429TooManyRequests;
        Response.Headers.RetryAfter = retryAfterSeconds.ToString();
        ViewBag.Message = $"Cok fazla hatali giris denemesi yapildi. Lutfen {Math.Ceiling(lockoutStatus.RetryAfter.TotalMinutes)} dakika sonra tekrar deneyin.";
    }
}
