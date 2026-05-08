using Microsoft.AspNetCore.Mvc;

namespace OfisYonetimSistemi.Controllers;

public class AssistantController : Controller
{
    public IActionResult Index()
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("RoleName")))
        {
            return RedirectToAction("Login", "Account");
        }

        return View();
    }
}
