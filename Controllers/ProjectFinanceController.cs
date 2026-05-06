using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfisYonetimSistemi.Models;

namespace OfisYonetimSistemi.Controllers;

public class ProjectFinanceController : Controller
{
    private readonly AppDbContext _context;

    public ProjectFinanceController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Select()
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        await FillSelectOptionsAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Select(int? projectId, int? companyId)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        if (!projectId.HasValue)
        {
            TempData["ErrorMessage"] = "Gelir-gider ekranini gormek icin bir proje secin.";
            return RedirectToAction(nameof(Select));
        }

        return RedirectToAction(nameof(Details), new { id = projectId.Value, companyId });
    }

    public async Task<IActionResult> Details(int id, int? companyId)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        var project = await _context.Projects
            .Include(p => p.Invoices)
                .ThenInclude(i => i.Company)
            .Include(p => p.StockMovements)
                .ThenInclude(sm => sm.Material)
            .Include(p => p.StockMovements)
                .ThenInclude(sm => sm.Company)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null)
        {
            return NotFound();
        }

        if (companyId.HasValue)
        {
            project.Invoices = project.Invoices
                .Where(i => i.CompanyId == companyId.Value)
                .ToList();

            project.StockMovements = project.StockMovements
                .Where(sm => sm.CompanyId == companyId.Value)
                .ToList();
        }

        var selectedCompany = companyId.HasValue
            ? await _context.Companies.FindAsync(companyId.Value)
            : null;

        ViewBag.SelectedCompanyName = selectedCompany?.Name;
        ViewBag.CompanyId = companyId;
        ViewBag.InvoiceExpense = project.Invoices.Sum(i => i.TotalAmount);
        ViewBag.StockExpense = project.StockMovements.Sum(sm => sm.TotalPrice);
        ViewBag.TotalExpense = ViewBag.InvoiceExpense + ViewBag.StockExpense;
        ViewBag.TotalIncome = 0m;
        ViewBag.Balance = ViewBag.TotalIncome - ViewBag.TotalExpense;

        return View(project);
    }

    private async Task FillSelectOptionsAsync()
    {
        var projects = await _context.Projects
            .OrderBy(p => p.Name)
            .ToListAsync();

        var companies = await _context.Companies
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.Projects = new SelectList(projects, "Id", "Name");
        ViewBag.Companies = new SelectList(companies, "Id", "Name");
    }

    private bool IsLoggedIn()
    {
        return !string.IsNullOrEmpty(HttpContext.Session.GetString("RoleName"));
    }
}
