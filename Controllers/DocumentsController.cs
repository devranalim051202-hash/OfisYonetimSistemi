using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfisYonetimSistemi.Models;

namespace OfisYonetimSistemi.Controllers;

public class DocumentsController : Controller
{
    private readonly AppDbContext _context;

    private static readonly string[] DocumentTypes =
    {
        "Fatura",
        "Fis",
        "Irsaliye",
        "Makbuz"
    };

    public DocumentsController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return RedirectToAction(nameof(Create));
    }

    public async Task<IActionResult> Create()
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        await FillFormOptionsAsync();
        return View(new Invoice { DocumentDate = DateTime.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Invoice invoice)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        if (!DocumentTypes.Contains(invoice.DocumentType))
        {
            ModelState.AddModelError(nameof(invoice.DocumentType), "Gecerli bir evrak turu secin.");
        }

        if (invoice.TotalAmount <= 0)
        {
            ModelState.AddModelError(nameof(invoice.TotalAmount), "Genel toplam 0'dan buyuk olmalidir.");
        }

        if (!ModelState.IsValid)
        {
            await FillFormOptionsAsync(invoice.DocumentType, invoice.ProjectId, invoice.CompanyId);
            return View(invoice);
        }

        invoice.CreatedAt = DateTime.Now;
        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Evrak kaydi olusturuldu.";
        return RedirectToAction("Index", "Expenses");
    }

    private async Task FillFormOptionsAsync(string? selectedDocumentType = null, int? selectedProjectId = null, int? selectedCompanyId = null)
    {
        var projects = await _context.Projects
            .OrderBy(p => p.Name)
            .ToListAsync();

        var companies = await _context.Companies
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.DocumentTypes = new SelectList(DocumentTypes, selectedDocumentType);
        ViewBag.Projects = new SelectList(projects, "Id", "Name", selectedProjectId);
        ViewBag.Companies = new SelectList(companies, "Id", "Name", selectedCompanyId);
    }

    private bool IsLoggedIn()
    {
        return !string.IsNullOrEmpty(HttpContext.Session.GetString("RoleName"));
    }
}
