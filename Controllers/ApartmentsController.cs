using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfisYonetimSistemi.Models;
using OfisYonetimSistemi.Models.ViewModels;
using OfisYonetimSistemi.Services;

namespace OfisYonetimSistemi.Controllers;

public class ApartmentsController : Controller
{
    private readonly AppDbContext _context;
    private readonly ActivityLogService _activityLogService;

    public ApartmentsController(AppDbContext context, ActivityLogService activityLogService)
    {
        _context = context;
        _activityLogService = activityLogService;
    }

    public async Task<IActionResult> Details(int id)
    {
        if (!IsManager())
        {
            await _activityLogService.LogAsync("YetkisizDeneme", "Daireler", id, "Daire satis sayfasina yetkisiz erisim denendi.", false);
            return RedirectToAction("Login", "Account");
        }

        var apartment = await _context.Apartments
            .Include(a => a.Project)
            .Include(a => a.Sale)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (apartment == null)
        {
            return NotFound();
        }

        return View(apartment);
    }

    public async Task<IActionResult> Sell(int id)
    {
        if (!IsManager())
        {
            await _activityLogService.LogAsync("YetkisizDeneme", "DaireSatislari", id, "Daire satis sayfasina yetkisiz erisim denendi.", false);
            return RedirectToAction("Login", "Account");
        }

        var apartment = await _context.Apartments
            .Include(a => a.Project)
            .Include(a => a.Sale)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (apartment == null)
        {
            return NotFound();
        }

        if (apartment.IsSold)
        {
            TempData["ErrorMessage"] = "Bu daire zaten satilmis.";
            return RedirectToAction(nameof(Details), new { id });
        }

        ViewBag.Apartment = apartment;
        return View(new ApartmentSaleViewModel
        {
            ApartmentId = apartment.Id,
            SalePrice = apartment.Price
        });
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!IsManager())
        {
            return RedirectToAction("Login", "Account");
        }

        var apartment = await _context.Apartments
            .Include(a => a.Project)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (apartment == null)
        {
            return NotFound();
        }

        return View(apartment);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Apartment apartment)
    {
        if (!IsManager())
        {
            return RedirectToAction("Login", "Account");
        }

        if (id != apartment.Id)
        {
            return NotFound();
        }

        if (apartment.GrossArea < 0)
        {
            ModelState.AddModelError(nameof(apartment.GrossArea), "Brut alan negatif olamaz.");
        }

        if (apartment.NetArea < 0)
        {
            ModelState.AddModelError(nameof(apartment.NetArea), "Net alan negatif olamaz.");
        }

        if (apartment.Price < 0)
        {
            ModelState.AddModelError(nameof(apartment.Price), "Fiyat negatif olamaz.");
        }

        if (!ModelState.IsValid)
        {
            apartment.Project = await _context.Projects.FindAsync(apartment.ProjectId);
            return View(apartment);
        }

        var existingApartment = await _context.Apartments.FindAsync(id);

        if (existingApartment == null)
        {
            return NotFound();
        }

        existingApartment.FloorNumber = apartment.FloorNumber;
        existingApartment.ApartmentNumber = apartment.ApartmentNumber;
        existingApartment.RoomType = apartment.RoomType;
        existingApartment.GrossArea = apartment.GrossArea;
        existingApartment.NetArea = apartment.NetArea;
        existingApartment.Price = apartment.Price;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Daire bilgileri guncellendi.";
        return RedirectToAction(nameof(Details), new { id = existingApartment.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sell(ApartmentSaleViewModel model)
    {
        if (!IsManager())
        {
            return RedirectToAction("Login", "Account");
        }

        var apartment = await _context.Apartments
            .Include(a => a.Project)
            .Include(a => a.Sale)
            .FirstOrDefaultAsync(a => a.Id == model.ApartmentId);

        if (apartment == null)
        {
            return NotFound();
        }

        if (apartment.IsSold || apartment.Sale != null)
        {
            ModelState.AddModelError(string.Empty, "Bu daire icin satis kaydi zaten var.");
        }

        if (model.SalePrice <= 0)
        {
            ModelState.AddModelError(nameof(model.SalePrice), "Satis bedeli sifirdan buyuk olmalidir.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Apartment = apartment;
            return View(model);
        }

        var saleDate = DateTime.Today;
        var contractText = BuildContractText(apartment, model, saleDate);

        var sale = new ApartmentSale
        {
            ApartmentId = apartment.Id,
            BuyerFullName = model.BuyerFullName,
            BuyerIdentityNumber = model.BuyerIdentityNumber,
            BuyerPhone = model.BuyerPhone,
            BuyerAddress = model.BuyerAddress,
            SalePrice = model.SalePrice,
            PaymentType = model.PaymentType,
            SaleDate = saleDate,
            ContractText = contractText,
            CreatedAt = DateTime.Now
        };

        apartment.IsSold = true;
        apartment.Price = model.SalePrice;

        _context.ApartmentSales.Add(sale);
        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync("Satis", "DaireSatislari", sale.Id, $"{apartment.Project?.Name ?? "-"} projesinde {apartment.ApartmentNumber} numarali daire {model.SalePrice:N2} TL bedelle satildi.");

        TempData["SuccessMessage"] = "Daire satisi tamamlandi ve sozlesme bilgisi olusturuldu.";
        return RedirectToAction(nameof(Details), new { id = apartment.Id });
    }

    public async Task<IActionResult> EditContract(int id)
    {
        if (!IsManager())
        {
            return RedirectToAction("Login", "Account");
        }

        var apartment = await _context.Apartments
            .Include(a => a.Project)
            .Include(a => a.Sale)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (apartment?.Sale == null)
        {
            return NotFound();
        }

        ViewBag.Apartment = apartment;
        return View(new ContractEditViewModel
        {
            ApartmentId = apartment.Id,
            ApartmentSaleId = apartment.Sale.Id,
            ContractText = apartment.Sale.ContractText
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditContract(ContractEditViewModel model)
    {
        if (!IsManager())
        {
            return RedirectToAction("Login", "Account");
        }

        var sale = await _context.ApartmentSales
            .Include(s => s.Apartment)
                .ThenInclude(a => a!.Project)
            .FirstOrDefaultAsync(s => s.Id == model.ApartmentSaleId && s.ApartmentId == model.ApartmentId);

        if (sale?.Apartment == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Apartment = sale.Apartment;
            return View(model);
        }

        sale.ContractText = model.ContractText;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Satis sozlesmesi guncellendi.";
        return RedirectToAction(nameof(Details), new { id = model.ApartmentId });
    }

    private string BuildContractText(Apartment apartment, ApartmentSaleViewModel sale, DateTime saleDate)
    {
        var sellerName = HttpContext.Session.GetString("CompanyName") ?? "Satici";
        var projectName = apartment.Project?.Name ?? "-";
        var location = apartment.Project?.Location ?? "-";

        var builder = new StringBuilder();
        builder.AppendLine("DAIRE SATIS SOZLESMESI");
        builder.AppendLine();
        builder.AppendLine($"Yer: {location}");
        builder.AppendLine($"Tarih: {saleDate:dd.MM.yyyy}");
        builder.AppendLine();
        builder.AppendLine("Satici Bilgileri:");
        builder.AppendLine($"Adi ve Soyadi / Unvan: {sellerName}");
        builder.AppendLine("TC Kimlik/Vergi No: ");
        builder.AppendLine($"Adres: {location}");
        builder.AppendLine();
        builder.AppendLine("Alici Bilgileri:");
        builder.AppendLine($"Adi ve Soyadi: {sale.BuyerFullName}");
        builder.AppendLine($"TC Kimlik/Vergi No: {sale.BuyerIdentityNumber}");
        builder.AppendLine($"Telefon: {sale.BuyerPhone}");
        builder.AppendLine($"Adres: {sale.BuyerAddress}");
        builder.AppendLine();
        builder.AppendLine("Satilan Daire Bilgileri:");
        builder.AppendLine($"Proje: {projectName}");
        builder.AppendLine($"Kat / Daire No: {apartment.FloorNumber} / {apartment.ApartmentNumber}");
        builder.AppendLine($"Oda Tipi: {apartment.RoomType}");
        builder.AppendLine($"Brut Alan (m2): {apartment.GrossArea:N2}");
        builder.AppendLine($"Net Alan (m2): {apartment.NetArea:N2}");
        builder.AppendLine("Iskan Durumu: ");
        builder.AppendLine();
        builder.AppendLine("Satis Bedeli ve Odeme Kosullari:");
        builder.AppendLine($"Satis Bedeli: {sale.SalePrice:N2} TL");
        builder.AppendLine($"Odeme Sekli: {sale.PaymentType}");
        builder.AppendLine();
        builder.AppendLine("Madde 1 - Sozlesmenin Konusu");
        builder.AppendLine("Satici, yukarida belirtilen daireyi aliciya satmayi, alici da saticidan satin almayi kabul eder.");
        builder.AppendLine();
        builder.AppendLine("Madde 2 - Mulkiyet ve Teslim");
        builder.AppendLine("Tasinmazin mulkiyeti, satis bedelinin tamaminin odenmesi ve teslim tutanaginin imzalanmasi ile aliciya gecer.");
        builder.AppendLine();
        builder.AppendLine("Madde 3 - Satis Bedeli ve Odeme");
        builder.AppendLine("Satis bedeli yukarida belirtilmis olup, odeme sekli ve taksitlendirme varsa bu sozlesmede belirtilir.");
        builder.AppendLine();
        builder.AppendLine("Madde 4 - Tasinmazin Durumu");
        builder.AppendLine("Satici, tasinmazin mevcut durumunu beyan etmis olup, alici incelemis ve kabul etmistir.");
        builder.AppendLine();
        builder.AppendLine("Madde 5 - Iskan ve Ruhsat");
        builder.AppendLine("Dairenin iskan belgesi ve ruhsat durumu satici tarafindan aliciya eksiksiz teslim edilir.");
        builder.AppendLine();
        builder.AppendLine("Madde 6 - Masraflar");
        builder.AppendLine("Tapu harci, vergi ve diger masraflar ilgili mevzuat uyarinca veya taraflarin anlasmasina gore odenir.");
        builder.AppendLine();
        builder.AppendLine("Madde 7 - Uyusmazliklarin Cozumu");
        builder.AppendLine("Bu sozlesmeden dogabilecek uyusmazliklarda taraflarin yerlesim yeri mahkemeleri ve icra daireleri yetkilidir.");
        builder.AppendLine();
        builder.AppendLine("SATAN                                      ALAN");
        builder.AppendLine();
        builder.AppendLine("Imza: ____________________                 Imza: ____________________");

        return builder.ToString();
    }

    private bool IsManager()
    {
        var roleName = HttpContext.Session.GetString("RoleName");
        return roleName == "Admin" || roleName == "Mudur";
    }
}
