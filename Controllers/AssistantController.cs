using Microsoft.AspNetCore.Mvc;
using OfisYonetimSistemi.Models.ViewModels;
using OfisYonetimSistemi.Services;

namespace OfisYonetimSistemi.Controllers;

public class AssistantController : Controller
{
    private readonly ChatBotCommandService _chatBotCommandService;

    public AssistantController(ChatBotCommandService chatBotCommandService)
    {
        _chatBotCommandService = chatBotCommandService;
    }

    public IActionResult Index()
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("RoleName")))
        {
            return RedirectToAction("Login", "Account");
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send([FromBody] ChatCommandRequest request)
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("RoleName")))
        {
            return Unauthorized(new ChatCommandResponse
            {
                Action = "Oturum",
                ResponseText = "Lutfen once sisteme giris yapin.",
                IsSuccessful = false
            });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(new ChatCommandResponse
            {
                Action = "Dogrulama",
                ResponseText = "Komut metni bos olamaz.",
                IsSuccessful = false
            });
        }

        var userId = HttpContext.Session.GetInt32("UserId");
        var roleName = HttpContext.Session.GetString("RoleName") ?? string.Empty;

        if (userId == null)
        {
            return Unauthorized(new ChatCommandResponse
            {
                Action = "Oturum",
                ResponseText = "Kullanici oturumu bulunamadi.",
                IsSuccessful = false
            });
        }

        try
        {
            var response = await _chatBotCommandService.ProcessAsync(request.CommandText, userId.Value, roleName);
            return Json(response);
        }
        catch
        {
            return Json(new ChatCommandResponse
            {
                Action = "SistemHatasi",
                ResponseText = "Uzgunum, su anda bu komutu isleyemiyorum. Lutfen farkli kelimelerle veya biraz daha kisa bir sekilde tekrar dener misiniz?",
                IsSuccessful = true
            });
        }
    }
}
