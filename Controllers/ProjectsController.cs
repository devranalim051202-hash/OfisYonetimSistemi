using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfisYonetimSistemi.Models;

namespace OfisYonetimSistemi.Controllers;

public class ProjectsController : Controller
{
    private readonly AppDbContext _context;

    private static readonly string[] ProjectStatuses =
    {
        "Aktif",
        "Beklemede",
        "Tamamlandi",
        "Iptal"
    };

    public ProjectsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        var projects = await _context.Projects
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return View(projects);
    }

    public async Task<IActionResult> Details(int id)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        var project = await _context.Projects
            .Include(p => p.StockMovements)
                .ThenInclude(sm => sm.Material)
            .Include(p => p.StockMovements)
                .ThenInclude(sm => sm.Company)
            .Include(p => p.Invoices)
                .ThenInclude(i => i.Company)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null)
        {
            return NotFound();
        }

        ViewBag.StockTotal = project.StockMovements.Sum(sm => sm.TotalPrice);
        ViewBag.InvoiceTotal = project.Invoices.Sum(i => i.TotalAmount);
        ViewBag.TotalExpense = ViewBag.StockTotal + ViewBag.InvoiceTotal;

        return View(project);
    }

    public IActionResult Create()
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        FillStatuses();
        return View(new Project { StartDate = DateTime.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Project project)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        if (!ProjectStatuses.Contains(project.Status))
        {
            ModelState.AddModelError(nameof(project.Status), "Gecerli bir proje durumu secin.");
        }

        if (!ModelState.IsValid)
        {
            FillStatuses(project.Status);
            return View(project);
        }

        project.CreatedAt = DateTime.Now;

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Proje kaydi olusturuldu.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        var project = await _context.Projects.FindAsync(id);

        if (project == null)
        {
            return NotFound();
        }

        FillStatuses(project.Status);
        return View(project);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Project project)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        if (id != project.Id)
        {
            return NotFound();
        }

        if (!ProjectStatuses.Contains(project.Status))
        {
            ModelState.AddModelError(nameof(project.Status), "Gecerli bir proje durumu secin.");
        }

        if (!ModelState.IsValid)
        {
            FillStatuses(project.Status);
            return View(project);
        }

        var existingProject = await _context.Projects.FindAsync(id);

        if (existingProject == null)
        {
            return NotFound();
        }

        existingProject.Name = project.Name;
        existingProject.Description = project.Description;
        existingProject.Location = project.Location;
        existingProject.StartDate = project.StartDate;
        existingProject.EndDate = project.EndDate;
        existingProject.Status = project.Status;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Proje kaydi guncellendi.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null)
        {
            return NotFound();
        }

        return View(project);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        var project = await _context.Projects
            .Include(p => p.StockMovements)
            .Include(p => p.Invoices)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null)
        {
            return NotFound();
        }

        if (project.StockMovements.Any() || project.Invoices.Any())
        {
            TempData["ErrorMessage"] = "Bu projeye bagli stok hareketi veya evrak oldugu icin silinemez.";
            return RedirectToAction(nameof(Index));
        }

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Proje kaydi silindi.";
        return RedirectToAction(nameof(Index));
    }

    private void FillStatuses(string? selectedStatus = null)
    {
        ViewBag.Statuses = new SelectList(ProjectStatuses, selectedStatus);
    }

    private bool IsLoggedIn()
    {
        return !string.IsNullOrEmpty(HttpContext.Session.GetString("RoleName"));
    }
}
