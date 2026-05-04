using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OfisYonetimSistemi.Models;

namespace OfisYonetimSistemi.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        var roleName = HttpContext.Session.GetString("RoleName");

        if (roleName == "Admin" || roleName == "Mudur")
        {
            return RedirectToAction("Index", "Manager");
        }

        if (!string.IsNullOrEmpty(roleName))
        {
            return RedirectToAction("Index", "Personnel");
        }

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
