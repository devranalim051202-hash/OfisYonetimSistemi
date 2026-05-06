using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfisYonetimSistemi.Models;

namespace OfisYonetimSistemi.Controllers;

public class ExpensesController : Controller
{
    private readonly AppDbContext _context;

    private static readonly string[] DocumentTypes =
    {
        "Fatura",
        "Fis",
        "Irsaliye",
        "Makbuz"
    };

    public ExpensesController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? documentType, int? projectId, DateTime? startDate, DateTime? endDate)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        var query = _context.Invoices
            .Include(i => i.Project)
            .Include(i => i.Company)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(documentType))
        {
            query = query.Where(i => i.DocumentType == documentType);
        }

        if (projectId.HasValue)
        {
            query = query.Where(i => i.ProjectId == projectId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(i => i.DocumentDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(i => i.DocumentDate <= endDate.Value);
        }

        var expenses = await query
            .OrderByDescending(i => i.DocumentDate)
            .ThenByDescending(i => i.CreatedAt)
            .ToListAsync();

        await FillFilterOptionsAsync(projectId);
        ViewBag.DocumentTypes = await _context.Invoices
            .Select(i => i.DocumentType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();
        ViewBag.DocumentType = documentType;
        ViewBag.ProjectId = projectId;
        ViewBag.StartDate = startDate;
        ViewBag.EndDate = endDate;

        return View(expenses);
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
            ModelState.AddModelError(nameof(invoice.DocumentType), "Gecerli bir belge turu secin.");
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

        TempData["SuccessMessage"] = "Gider kaydi olusturuldu.";
        return RedirectToAction(nameof(Index));
    }

    private async Task FillFilterOptionsAsync(int? selectedProjectId)
    {
        var projects = await _context.Projects
            .OrderBy(p => p.Name)
            .ToListAsync();

        ViewBag.Projects = new SelectList(projects, "Id", "Name", selectedProjectId);
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
