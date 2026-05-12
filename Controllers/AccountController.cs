using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfisYonetimSistemi.Models;
using OfisYonetimSistemi.Models.ViewModels;
using OfisYonetimSistemi.Services;
using System.Net;
using System.Text.Json;

namespace OfisYonetimSistemi.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _context;
    private readonly ActivityLogService _activityLogService;
    private readonly LoginAttemptTracker _loginAttemptTracker;
    private readonly IEmailSender _emailSender;

    public AccountController(
        AppDbContext context,
        ActivityLogService activityLogService,
        LoginAttemptTracker loginAttemptTracker,
        IEmailSender emailSender)
    {
        _context = context;
        _activityLogService = activityLogService;
        _loginAttemptTracker = loginAttemptTracker;
        _emailSender = emailSender;
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
            ModelState.AddModelError(nameof(model.Email), "Bu mail adresi zaten kullanılıyor.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var verificationCode = Random.Shared.Next(100000, 999999).ToString();

        HttpContext.Session.SetString("PendingRegister", JsonSerializer.Serialize(model));
        HttpContext.Session.SetString("VerificationCode", verificationCode);

        try
        {
            await _emailSender.SendEmailAsync(
                model.Email,
                "Smart Office mail doğrulama kodu",
                BuildVerificationEmailBody(model.FirstName, verificationCode));
        }
        catch (InvalidOperationException ex)
        {
            HttpContext.Session.Remove("PendingRegister");
            HttpContext.Session.Remove("VerificationCode");
            ModelState.AddModelError(nameof(model.Email), ex.Message);
            return View(model);
        }
        catch (System.Net.Mail.SmtpException)
        {
            HttpContext.Session.Remove("PendingRegister");
            HttpContext.Session.Remove("VerificationCode");
            ModelState.AddModelError(nameof(model.Email), "Mail gönderilemedi. Gmail uygulama şifresini, 2 adımlı doğrulamayı ve SMTP ayarlarını kontrol edin.");
            return View(model);
        }
        catch (Exception)
        {
            HttpContext.Session.Remove("PendingRegister");
            HttpContext.Session.Remove("VerificationCode");
            ModelState.AddModelError(nameof(model.Email), "Doğrulama maili gönderilirken beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.");
            return View(model);
        }

        TempData["VerificationInfo"] = "Doğrulama kodu mail adresinize gönderildi.";
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
            ModelState.AddModelError(nameof(model.Code), "Doğrulama kodu hatalı.");
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
            ModelState.AddModelError(nameof(RegisterViewModel.Email), "Bu mail adresi zaten kullanılıyor.");
            return View(nameof(Register), registerModel);
        }

        HttpContext.Session.Remove("PendingRegister");
        HttpContext.Session.Remove("VerificationCode");
        user.Role = await _context.Roles.FindAsync(user.RoleId);
        SetSession(user);

        TempData["SuccessMessage"] = "Üyelik oluşturuldu. Yeni şirket hesabınız açıldı.";
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

    private static string BuildVerificationEmailBody(string firstName, string verificationCode)
    {
        return $"""
            <div style="font-family:Arial,sans-serif;max-width:560px;margin:auto;padding:24px;border:1px solid #e5e7eb;border-radius:12px">
                <h2 style="margin:0 0 12px;color:#0f172a">Smart Office Mail Doğrulama</h2>
                <p>Merhaba {WebUtility.HtmlEncode(firstName)},</p>
                <p>Smart Office hesabını oluşturmak için doğrulama kodun:</p>
                <div style="font-size:32px;font-weight:800;letter-spacing:6px;background:#f1f5f9;padding:18px 20px;border-radius:10px;text-align:center;color:#2563eb">
                    {verificationCode}
                </div>
                <p style="color:#475569;margin-top:18px">Bu kodu hesap doğrulama ekranına girerek üyeliği tamamlayabilirsin.</p>
                <p style="color:#64748b;font-size:13px">Bu işlemi sen başlatmadıysan bu maili dikkate alma.</p>
            </div>
            """;
    }
}
