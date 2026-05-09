using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfisYonetimSistemi.Models;
using OfisYonetimSistemi.Models.ViewModels;
using OfisYonetimSistemi.Services;

namespace OfisYonetimSistemi.Controllers;

public class ProjectsController : Controller
{
    private readonly AppDbContext _context;
    private readonly ActivityLogService _activityLogService;
    private readonly IProjectImageService _projectImageService;
    private readonly ILogger<ProjectsController> _logger;

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

    public ProjectsController(
        AppDbContext context,
        ActivityLogService activityLogService,
        IProjectImageService projectImageService,
        ILogger<ProjectsController> logger)
    {
        _context = context;
        _activityLogService = activityLogService;
        _projectImageService = projectImageService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        var projects = await _context.Projects
            .Include(p => p.ProjectImages)
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
            .Include(p => p.Apartments)
                .ThenInclude(a => a.Sale)
            .Include(p => p.ProjectImages)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null)
        {
            return NotFound();
        }

        ViewBag.InvoiceTotal = project.Invoices.Sum(i => i.TotalAmount);
        ViewBag.DirectExpenseTotal = project.Expenses.Sum(e => e.Amount);
        ViewBag.TotalExpense = ViewBag.DirectExpenseTotal;
        ViewBag.CanManageApartmentSales = IsManager();

        return View(project);
    }

    public IActionResult Create()
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        FillStatuses();
        return View(new ProjectFormViewModel { StartDate = DateTime.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectFormViewModel model)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        if (!ProjectStatuses.Contains(model.Status))
        {
            ModelState.AddModelError(nameof(model.Status), "Gecerli bir proje durumu secin.");
        }

        ValidateProjectDates(model);
        ValidateApartmentPlan(model);

        if (!ModelState.IsValid)
        {
            FillStatuses(model.Status);
            return View(model);
        }

        var project = model.ToProject();
        project.CreatedAt = DateTime.Now;

        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            await _projectImageService.AddImagesAsync(project.Id, model.ProjectImages, model.MakeFirstImageCover);
            await _activityLogService.LogAsync("Ekleme", "Projeler", project.Id, $"{project.Name} projesi olusturuldu.");
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Project could not be created with images.");
            ModelState.AddModelError(string.Empty, "Proje kaydi olusturulurken hata olustu.");
            FillStatuses(model.Status);
            return View(model);
        }

        if (project.ApartmentCount > 0)
        {
            await CreateMissingApartmentsAsync(project);
        }

        TempData["SuccessMessage"] = "Proje kaydi olusturuldu.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        var project = await _context.Projects
            .Include(p => p.ProjectImages)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null)
        {
            return NotFound();
        }

        FillStatuses(project.Status);
        return View(ProjectFormViewModel.FromProject(project));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProjectFormViewModel model)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ProjectStatuses.Contains(model.Status))
        {
            ModelState.AddModelError(nameof(model.Status), "Gecerli bir proje durumu secin.");
        }

        ValidateProjectDates(model);
        ValidateApartmentPlan(model);

        if (!ModelState.IsValid)
        {
            FillStatuses(model.Status);
            await FillExistingImagesAsync(model);
            return View(model);
        }

        var existingProject = await _context.Projects
            .Include(p => p.ProjectImages)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (existingProject == null)
        {
            return NotFound();
        }

        existingProject.Name = model.Name;
        existingProject.Description = model.Description;
        existingProject.Location = model.Location;
        existingProject.StartDate = model.StartDate;
        existingProject.EndDate = model.EndDate;
        existingProject.Status = model.Status;
        existingProject.FloorCount = model.FloorCount;
        existingProject.ApartmentCount = model.ApartmentCount;

        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            await _context.SaveChangesAsync();
            await _projectImageService.SetCoverImageAsync(existingProject.Id, model.CoverImageId);
            await _projectImageService.AddImagesAsync(existingProject.Id, model.ProjectImages, model.MakeFirstImageCover);
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Project {ProjectId} could not be updated with images.", existingProject.Id);
            ModelState.AddModelError(string.Empty, "Proje kaydi guncellenirken hata olustu.");
            FillStatuses(model.Status);
            await FillExistingImagesAsync(model);
            return View(model);
        }

        if (existingProject.ApartmentCount > 0)
        {
            await CreateMissingApartmentsAsync(existingProject);
        }

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
            .Include(p => p.Apartments)
            .Include(p => p.ProjectImages)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null)
        {
            return NotFound();
        }

        if (project.StockMovements.Any() || project.Invoices.Any() || project.Expenses.Any() || project.Apartments.Any())
        {
            TempData["ErrorMessage"] = "Bu projeye bagli stok hareketi, gider, evrak veya daire kaydi oldugu icin silinemez.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _projectImageService.DeleteImagesForProjectAsync(project.Id);
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Project {ProjectId} could not be deleted with images.", project.Id);
            TempData["ErrorMessage"] = "Proje silinirken hata olustu.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = "Proje kaydi silindi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int id)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        var image = await _context.ProjectImages.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);

        if (image == null)
        {
            return NotFound();
        }

        try
        {
            await _projectImageService.DeleteImageAsync(id);
            TempData["SuccessMessage"] = "Proje gorseli silindi.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Project image {ProjectImageId} could not be deleted.", id);
            TempData["ErrorMessage"] = "Proje gorseli silinirken hata olustu.";
        }

        return RedirectToAction(nameof(Edit), new { id = image.ProjectId });
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
        await _activityLogService.LogAsync("EvrakYukleme", "Evraklar", invoice.Id, $"{project.Name} projesine evrak yuklendi: {invoice.DocumentNumber}");

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkUpdateApartmentRoomType(int projectId, string selectionType, int selectionNumber, string roomType)
    {
        if (!IsManager())
        {
            await _activityLogService.LogAsync("YetkisizDeneme", "Daireler", projectId, "Daire oda tipi toplu guncelleme yetkisiz denendi.", false);
            return RedirectToAction("Login", "Account");
        }

        if (string.IsNullOrWhiteSpace(roomType))
        {
            TempData["ErrorMessage"] = "Oda tipi bos olamaz.";
            return RedirectToAction(nameof(Details), new { id = projectId });
        }

        var apartments = await _context.Apartments
            .Where(a => a.ProjectId == projectId)
            .OrderBy(a => a.FloorNumber)
            .ThenBy(a => a.ApartmentNumber)
            .ToListAsync();

        if (!apartments.Any())
        {
            TempData["ErrorMessage"] = "Bu proje icin daire kaydi bulunmuyor.";
            return RedirectToAction(nameof(Details), new { id = projectId });
        }

        var selectedApartments = selectionType == "row"
            ? apartments.Where(a => a.FloorNumber == selectionNumber).ToList()
            : apartments
                .GroupBy(a => a.FloorNumber)
                .SelectMany(g => g.OrderBy(a => a.ApartmentNumber)
                    .Select((apartment, index) => new { apartment, columnNumber = index + 1 })
                    .Where(x => x.columnNumber == selectionNumber)
                    .Select(x => x.apartment))
                .ToList();

        if (!selectedApartments.Any())
        {
            TempData["ErrorMessage"] = "Secilen satir veya sutunda daire bulunamadi.";
            return RedirectToAction(nameof(Details), new { id = projectId });
        }

        foreach (var apartment in selectedApartments)
        {
            apartment.RoomType = roomType.Trim();
        }

        await _context.SaveChangesAsync();

        var selectionLabel = selectionType == "row" ? $"{selectionNumber}. kat" : $"{selectionNumber}. sutun";
        TempData["SuccessMessage"] = $"{selectionLabel} icin {selectedApartments.Count} dairenin oda tipi guncellendi.";
        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    private void FillStatuses(string? selectedStatus = null)
    {
        ViewBag.Statuses = new SelectList(ProjectStatuses, selectedStatus);
    }

    private void ValidateProjectDates(ProjectFormViewModel project)
    {
        if (project.EndDate.HasValue && project.EndDate.Value.Date < project.StartDate.Date)
        {
            ModelState.AddModelError(nameof(project.EndDate), "Bitis tarihi baslangic tarihinden once olamaz.");
        }
    }

    private void ValidateApartmentPlan(ProjectFormViewModel project)
    {
        if (project.ApartmentCount > 0 && project.FloorCount <= 0)
        {
            ModelState.AddModelError(nameof(project.FloorCount), "Daire olusturmak icin kat sayisi girin.");
        }
    }

    private async Task CreateMissingApartmentsAsync(Project project)
    {
        var existingCount = await _context.Apartments.CountAsync(a => a.ProjectId == project.Id);
        var missingCount = project.ApartmentCount - existingCount;

        if (missingCount <= 0)
        {
            return;
        }

        var floorCount = Math.Max(project.FloorCount, 1);
        var startIndex = existingCount + 1;

        for (var offset = 0; offset < missingCount; offset++)
        {
            var apartmentIndex = startIndex + offset;
            var zeroBased = apartmentIndex - 1;
            var floorNumber = zeroBased % floorCount + 1;
            var numberOnFloor = zeroBased / floorCount + 1;

            _context.Apartments.Add(new Apartment
            {
                ProjectId = project.Id,
                FloorNumber = floorNumber,
                ApartmentNumber = $"{floorNumber}{numberOnFloor:00}",
                RoomType = "1+1",
                GrossArea = 0,
                NetArea = 0,
                Price = 0,
                CreatedAt = DateTime.Now
            });
        }

        await _context.SaveChangesAsync();
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

    private async Task FillExistingImagesAsync(ProjectFormViewModel model)
    {
        model.ExistingImages = await _context.ProjectImages
            .Where(i => i.ProjectId == model.Id)
            .OrderByDescending(i => i.IsCover)
            .ThenBy(i => i.CreatedAt)
            .ToListAsync();
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

    private bool IsManager()
    {
        var roleName = HttpContext.Session.GetString("RoleName");
        return roleName == "Admin" || roleName == "Mudur";
    }
}
