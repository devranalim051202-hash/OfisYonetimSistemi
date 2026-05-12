using Microsoft.AspNetCore.Mvc;
using OfisYonetimSistemi.Localization;

namespace OfisYonetimSistemi.Controllers;

public class LanguageController : Controller
{
    public IActionResult Set(string culture, string? returnUrl = null)
    {
        var selectedCulture = string.Equals(culture, AppLanguage.English, StringComparison.OrdinalIgnoreCase)
            ? AppLanguage.English
            : AppLanguage.Turkish;

        Response.Cookies.Append(
            AppLanguage.CookieName,
            selectedCulture,
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = false,
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }
}
