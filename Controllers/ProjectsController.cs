using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfisYonetimSistemi.Models;
using OfisYonetimSistemi.Models.ViewModels;

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

    private static readonly string[] DocumentTypes =
    {
        "PDF",
        "Gorsel"
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
            .Include(p => p.Expenses)
                .ThenInclude(e => e.Company)
            .Include(p => p.Expenses)
                .ThenInclude(e => e.User)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null)
        {
            return NotFound();
        }

        ViewBag.InvoiceTotal = project.Invoices.Sum(i => i.TotalAmount);
        ViewBag.DirectExpenseTotal = project.Expenses.Sum(e => e.Amount);
        ViewBag.TotalExpense = ViewBag.DirectExpenseTotal;

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

        ValidateProjectDates(project);

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

        ValidateProjectDates(project);

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
            .Include(p => p.Expenses)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null)
        {
            return NotFound();
        }

        if (project.StockMovements.Any() || project.Invoices.Any() || project.Expenses.Any())
        {
            TempData["ErrorMessage"] = "Bu projeye bagli stok hareketi, gider veya evrak oldugu icin silinemez.";
            return RedirectToAction(nameof(Index));
        }

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Proje kaydi silindi.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> AddMaterial(int projectId)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        var project = await _context.Projects.FindAsync(projectId);

        if (project == null)
        {
            return NotFound();
        }

        await FillProjectMaterialFormAsync(project);
        return View(new ProjectMaterialUsageViewModel { ProjectId = projectId, MovementDate = DateTime.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMaterial(ProjectMaterialUsageViewModel model)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        var project = await _context.Projects.FindAsync(model.ProjectId);

        if (project == null)
        {
            return NotFound();
        }

        var material = await _context.Materials.FindAsync(model.MaterialId);

        if (material == null)
        {
            ModelState.AddModelError(nameof(model.MaterialId), "Gecerli bir malzeme secin.");
        }
        else if (model.Quantity > material.CurrentStock)
        {
            ModelState.AddModelError(nameof(model.Quantity), "Kullanilan miktar mevcut stoktan fazla olamaz.");
        }

        if (!ModelState.IsValid)
        {
            await FillProjectMaterialFormAsync(project, model.MaterialId, model.CompanyId);
            return View(model);
        }

        var movement = new StockMovement
        {
            MaterialId = model.MaterialId,
            ProjectId = model.ProjectId,
            CompanyId = model.CompanyId,
            MovementType = "Cikis",
            Quantity = model.Quantity,
            UnitPrice = model.UnitPrice,
            TotalPrice = model.Quantity * model.UnitPrice,
            MovementDate = DateTime.Today,
            Description = $"{project.Name} projesinde kullanildi.",
            CreatedAt = DateTime.Now
        };

        material!.CurrentStock -= model.Quantity;
        _context.StockMovements.Add(movement);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Projeye malzeme kullanimi eklendi.";
        return RedirectToAction(nameof(Details), new { id = model.ProjectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMaterial(int id)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        var movement = await _context.StockMovements
            .Include(sm => sm.Material)
            .FirstOrDefaultAsync(sm => sm.Id == id);

        if (movement == null)
        {
            return NotFound();
        }

        var projectId = movement.ProjectId;

        if (movement.MovementType == "Cikis" && movement.Material != null)
        {
            movement.Material.CurrentStock += movement.Quantity;
        }

        _context.StockMovements.Remove(movement);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Projeden malzeme kullanimi kaldirildi.";
        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    public async Task<IActionResult> AddDocument(int projectId)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        var project = await _context.Projects.FindAsync(projectId);

        if (project == null)
        {
            return NotFound();
        }

        ViewBag.Project = project;
        return View(new ProjectDocumentViewModel { ProjectId = projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDocument(ProjectDocumentViewModel model)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        var project = await _context.Projects.FindAsync(model.ProjectId);

        if (project == null)
        {
            return NotFound();
        }

        if (model.DocumentFile == null)
        {
            ModelState.AddModelError(nameof(model.DocumentFile), "PDF veya gorsel dosyasi secin.");
        }
        else if (!IsAllowedProjectDocument(model.DocumentFile))
        {
            ModelState.AddModelError(nameof(model.DocumentFile), "Sadece PDF veya gorsel dosyasi yuklenebilir.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Project = project;
            return View(model);
        }

        var uploadResult = await SaveProjectDocumentAsync(model.DocumentFile!);

        var invoice = new Invoice
        {
            ProjectId = model.ProjectId,
            DocumentType = uploadResult.DocumentType,
            DocumentNumber = model.DocumentNumber,
            DocumentDate = DateTime.Today,
            SubTotal = 0,
            TaxAmount = 0,
            TotalAmount = 0,
            Description = model.Description,
            FilePath = uploadResult.FilePath,
            CreatedAt = DateTime.Now
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Projeye evrak eklendi.";
        return RedirectToAction(nameof(Details), new { id = model.ProjectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveDocument(int id)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        var invoice = await _context.Invoices.FindAsync(id);

        if (invoice == null)
        {
            return NotFound();
        }

        var projectId = invoice.ProjectId;

        _context.Invoices.Remove(invoice);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Proje evraki kaldirildi.";
        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    private void FillStatuses(string? selectedStatus = null)
    {
        ViewBag.Statuses = new SelectList(ProjectStatuses, selectedStatus);
    }

    private void ValidateProjectDates(Project project)
    {
        if (project.EndDate.HasValue && project.EndDate.Value.Date < project.StartDate.Date)
        {
            ModelState.AddModelError(nameof(project.EndDate), "Bitis tarihi baslangic tarihinden once olamaz.");
        }
    }

    private async Task FillProjectMaterialFormAsync(Project project, int? materialId = null, int? companyId = null)
    {
        var materials = await _context.Materials
            .OrderBy(m => m.Name)
            .ToListAsync();

        var companies = await _context.Companies
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.Project = project;
        ViewBag.Materials = new SelectList(materials, "Id", "Name", materialId);
        ViewBag.Companies = new SelectList(companies, "Id", "Name", companyId);
    }

    private static bool IsAllowedProjectDocument(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".webp" };

        return file.Length > 0 && allowedExtensions.Contains(extension);
    }

    private static async Task<(string FilePath, string OriginalFileName, string DocumentType)> SaveProjectDocumentAsync(IFormFile file)
    {
        var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "project-documents");
        Directory.CreateDirectory(uploadsPath);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var safeFileName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(uploadsPath, safeFileName);

        await using var stream = System.IO.File.Create(physicalPath);
        await file.CopyToAsync(stream);

        var documentType = extension == ".pdf" ? "PDF" : "Gorsel";

        return ($"/uploads/project-documents/{safeFileName}", Path.GetFileName(file.FileName), documentType);
    }

    private bool IsLoggedIn()
    {
        return !string.IsNullOrEmpty(HttpContext.Session.GetString("RoleName"));
    }
}
