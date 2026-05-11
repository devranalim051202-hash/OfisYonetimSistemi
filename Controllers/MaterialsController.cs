using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfisYonetimSistemi.Models;
using OfisYonetimSistemi.Models.ViewModels;
using OfisYonetimSistemi.Security;

namespace OfisYonetimSistemi.Controllers;

public class MaterialsController : Controller
{
    private readonly AppDbContext _context;

    public MaterialsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        if (!CanUseMaterials())
        {
            return RedirectToAction("Login", "Account");
        }

        var materials = await _context.Materials
            .OrderBy(m => m.Name)
            .ToListAsync();

        ViewBag.TotalMaterialCount = materials.Count;
        ViewBag.CriticalMaterialCount = materials.Count(m => m.CurrentStock <= m.MinimumStockLevel);

        return View(materials);
    }

    public async Task<IActionResult> Details(int id)
    {
        if (!CanUseMaterials())
        {
            return RedirectToAction("Login", "Account");
        }

        var material = await _context.Materials
            .Include(m => m.StockMovements)
                .ThenInclude(sm => sm.Project)
            .Include(m => m.StockMovements)
                .ThenInclude(sm => sm.Company)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (material == null)
        {
            return NotFound();
        }

        ViewBag.TotalIn = material.StockMovements
            .Where(sm => sm.MovementType == "Giris")
            .Sum(sm => sm.Quantity);

        ViewBag.TotalOut = material.StockMovements
            .Where(sm => sm.MovementType == "Cikis")
            .Sum(sm => sm.Quantity);

        return View(material);
    }

    public IActionResult Create()
    {
        if (!CanUseMaterials())
        {
            return RedirectToAction("Login", "Account");
        }

        return View(new Material());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Material material)
    {
        if (!CanUseMaterials())
        {
            return RedirectToAction("Login", "Account");
        }

        if (!ModelState.IsValid)
        {
            return View(material);
        }

        material.CreatedAt = DateTime.Now;
        _context.Materials.Add(material);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Malzeme kaydi olusturuldu.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!CanUseMaterials())
        {
            return RedirectToAction("Login", "Account");
        }

        var material = await _context.Materials.FindAsync(id);

        if (material == null)
        {
            return NotFound();
        }

        return View(material);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Material material)
    {
        if (!CanUseMaterials())
        {
            return RedirectToAction("Login", "Account");
        }

        if (id != material.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(material);
        }

        var existingMaterial = await _context.Materials.FindAsync(id);

        if (existingMaterial == null)
        {
            return NotFound();
        }

        existingMaterial.Name = material.Name;
        existingMaterial.Category = material.Category;
        existingMaterial.Unit = material.Unit;
        existingMaterial.MinimumStockLevel = material.MinimumStockLevel;
        existingMaterial.Description = material.Description;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Malzeme kaydi guncellendi.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        if (!CanUseMaterials())
        {
            return RedirectToAction("Login", "Account");
        }

        var material = await _context.Materials
            .Include(m => m.StockMovements)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (material == null)
        {
            return NotFound();
        }

        return View(material);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!CanUseMaterials())
        {
            return RedirectToAction("Login", "Account");
        }

        var material = await _context.Materials
            .Include(m => m.StockMovements)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (material == null)
        {
            return NotFound();
        }

        if (material.StockMovements.Any())
        {
            TempData["ErrorMessage"] = "Bu malzemeye ait stok hareketi oldugu icin silinemez.";
            return RedirectToAction(nameof(Index));
        }

        _context.Materials.Remove(material);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Malzeme kaydi silindi.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> StockIn(int id)
    {
        if (!CanUseMaterials())
        {
            return RedirectToAction("Login", "Account");
        }

        var material = await _context.Materials.FindAsync(id);

        if (material == null)
        {
            return NotFound();
        }

        ViewBag.Material = material;
        return View(new StockMovementFormViewModel { MaterialId = material.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StockIn(StockMovementFormViewModel model)
    {
        if (!CanUseMaterials())
        {
            return RedirectToAction("Login", "Account");
        }

        var material = await _context.Materials.FindAsync(model.MaterialId);

        if (material == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Material = material;
            return View(model);
        }

        var movement = CreateMovement(model, "Giris");

        material.CurrentStock += model.Quantity;
        _context.StockMovements.Add(movement);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Stok girisi kaydedildi.";
        return RedirectToAction(nameof(Details), new { id = material.Id });
    }

    public async Task<IActionResult> StockOut(int id)
    {
        if (!CanUseMaterials())
        {
            return RedirectToAction("Login", "Account");
        }

        var material = await _context.Materials.FindAsync(id);

        if (material == null)
        {
            return NotFound();
        }

        await FillProjectsAsync();
        ViewBag.Material = material;
        return View(new StockMovementFormViewModel { MaterialId = material.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StockOut(StockMovementFormViewModel model)
    {
        if (!CanUseMaterials())
        {
            return RedirectToAction("Login", "Account");
        }

        var material = await _context.Materials.FindAsync(model.MaterialId);

        if (material == null)
        {
            return NotFound();
        }

        if (model.Quantity > material.CurrentStock)
        {
            ModelState.AddModelError(nameof(model.Quantity), "Cikis miktari mevcut stoktan fazla olamaz.");
        }

        if (!ModelState.IsValid)
        {
            await FillProjectsAsync();
            ViewBag.Material = material;
            return View(model);
        }

        var movement = CreateMovement(model, "Cikis");

        material.CurrentStock -= model.Quantity;
        _context.StockMovements.Add(movement);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Stok cikisi kaydedildi.";
        return RedirectToAction(nameof(Details), new { id = material.Id });
    }

    public async Task<IActionResult> History()
    {
        if (!CanUseMaterials())
        {
            return RedirectToAction("Login", "Account");
        }

        var movements = await _context.StockMovements
            .Include(sm => sm.Material)
            .Include(sm => sm.Project)
            .Include(sm => sm.Company)
            .OrderByDescending(sm => sm.MovementDate)
            .ThenByDescending(sm => sm.CreatedAt)
            .ToListAsync();

        return View(movements);
    }

    private StockMovement CreateMovement(StockMovementFormViewModel model, string movementType)
    {
        return new StockMovement
        {
            MaterialId = model.MaterialId,
            ProjectId = movementType == "Cikis" ? model.ProjectId : null,
            MovementType = movementType,
            Quantity = model.Quantity,
            UnitPrice = model.UnitPrice,
            TotalPrice = model.Quantity * model.UnitPrice,
            MovementDate = model.MovementDate,
            DocumentNumber = model.DocumentNumber,
            Description = model.Description,
            CreatedAt = DateTime.Now
        };
    }

    private async Task FillProjectsAsync()
    {
        var projects = await _context.Projects
            .OrderBy(p => p.Name)
            .ToListAsync();

        ViewBag.Projects = new SelectList(projects, "Id", "Name");
    }

    private bool CanUseMaterials()
    {
        return RolePermissions.CanManageProjectMaterials(HttpContext.Session.GetString("RoleName"));
    }
}
