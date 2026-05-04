using Microsoft.AspNetCore.Mvc;

namespace OfisYonetimSistemi.Controllers;

public class PersonnelController : Controller
{
    public IActionResult Index()
    {
        var roleName = HttpContext.Session.GetString("RoleName");

        if (string.IsNullOrEmpty(roleName) || roleName == "Admin" || roleName == "Mudur")
        {
            return RedirectToAction("Login", "Account");
        }

        return View();
    }
}
