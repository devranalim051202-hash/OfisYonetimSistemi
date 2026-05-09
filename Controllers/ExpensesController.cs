using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfisYonetimSistemi.Models;
using OfisYonetimSistemi.Services;

namespace OfisYonetimSistemi.Controllers;

public class ExpensesController : Controller
{
    private readonly AppDbContext _context;
    private readonly ActivityLogService _activityLogService;

    public ExpensesController(AppDbContext context, ActivityLogService activityLogService)
    {
        _context = context;
        _activityLogService = activityLogService;
    }

    public IActionResult Index()
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        return RedirectToAction("Index", "Projects");
    }

    public async Task<IActionResult> Details(int id)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        var expense = await _context.Expenses
            .Include(e => e.Project)
            .Include(e => e.Company)
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (expense == null)
        {
            return NotFound();
        }

        return View(expense);
    }

    public async Task<IActionResult> Create(int? projectId = null)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        if (!projectId.HasValue)
        {
            TempData["ErrorMessage"] = "Gider eklemek icin once proje detayina girin.";
            return RedirectToAction("Index", "Projects");
        }

        var projectExists = await _context.Projects.AnyAsync(p => p.Id == projectId.Value);

        if (!projectExists)
        {
            return NotFound();
        }

        ViewBag.Project = await _context.Projects.FindAsync(projectId.Value);
        ViewBag.IsProjectScoped = true;
        return View(new Expense { ProjectId = projectId, ExpenseDate = DateTime.Today, ExpenseType = "Malzeme", PaymentStatus = "Odendi" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Expense expense)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        SetCurrentUser(expense);
        NormalizeExpense(expense);
        ClearDocumentMetadataValidation();

        if (!expense.ProjectId.HasValue)
        {
            ModelState.AddModelError(nameof(expense.ProjectId), "Gider kaydi bir projeye bagli olmalidir.");
        }

        if (expense.DocumentFile == null)
        {
            ModelState.AddModelError(nameof(expense.DocumentFile), "Fis / belge dosyasi zorunludur.");
        }
        else if (!IsAllowedExpenseDocument(expense.DocumentFile))
        {
            ModelState.AddModelError(nameof(expense.DocumentFile), "Sadece PDF veya gorsel dosyasi yukleyebilirsiniz.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Project = expense.ProjectId.HasValue
                ? await _context.Projects.FindAsync(expense.ProjectId.Value)
                : null;
            ViewBag.IsProjectScoped = expense.ProjectId.HasValue;
            return View(expense);
        }

        var document = await SaveExpenseDocumentAsync(expense.DocumentFile!);
        expense.DocumentFilePath = document.FilePath;
        expense.DocumentOriginalFileName = document.OriginalFileName;
        expense.DocumentContentType = document.ContentType;
        expense.DocumentNumber = null;
        expense.CreatedAt = DateTime.Now;

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync("Ekleme", "Giderler", expense.Id, $"{expense.Title} gideri eklendi. Tutar: {expense.Amount:N2} TL");

        TempData["SuccessMessage"] = "Gider kaydi olusturuldu.";
        if (expense.ProjectId.HasValue)
        {
            return RedirectToAction("Details", "Projects", new { id = expense.ProjectId.Value });
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        var expense = await _context.Expenses.FindAsync(id);

        if (expense == null)
        {
            return NotFound();
        }

        ViewBag.IsProjectScoped = expense.ProjectId.HasValue;
        ViewBag.Project = expense.ProjectId.HasValue
            ? await _context.Projects.FindAsync(expense.ProjectId.Value)
            : null;
        return View(expense);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Expense expense)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        if (id != expense.Id)
        {
            return NotFound();
        }

        SetCurrentUser(expense);
        NormalizeExpense(expense);
        ClearDocumentMetadataValidation();

        var existingExpense = await _context.Expenses.FindAsync(id);

        if (existingExpense == null)
        {
            return NotFound();
        }

        if (expense.DocumentFile != null && !IsAllowedExpenseDocument(expense.DocumentFile))
        {
            ModelState.AddModelError(nameof(expense.DocumentFile), "Sadece PDF veya gorsel dosyasi yukleyebilirsiniz.");
        }
        else if (expense.DocumentFile == null && string.IsNullOrWhiteSpace(existingExpense.DocumentFilePath))
        {
            ModelState.AddModelError(nameof(expense.DocumentFile), "Fis / belge dosyasi zorunludur.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.IsProjectScoped = expense.ProjectId.HasValue;
            ViewBag.Project = expense.ProjectId.HasValue
                ? await _context.Projects.FindAsync(expense.ProjectId.Value)
                : null;
            expense.DocumentFilePath = existingExpense.DocumentFilePath;
            expense.DocumentOriginalFileName = existingExpense.DocumentOriginalFileName;
            expense.DocumentContentType = existingExpense.DocumentContentType;
            return View(expense);
        }

        if (expense.DocumentFile != null)
        {
            var document = await SaveExpenseDocumentAsync(expense.DocumentFile);
            existingExpense.DocumentFilePath = document.FilePath;
            existingExpense.DocumentOriginalFileName = document.OriginalFileName;
            existingExpense.DocumentContentType = document.ContentType;
        }

        existingExpense.ProjectId = expense.ProjectId;
        existingExpense.CompanyId = expense.CompanyId;
        existingExpense.UserId = expense.UserId;
        existingExpense.Title = expense.Title;
        existingExpense.SupplierName = expense.SupplierName;
        existingExpense.ExpenseType = expense.ExpenseType;
        existingExpense.Quantity = expense.Quantity;
        existingExpense.UnitPrice = expense.UnitPrice;
        existingExpense.Amount = expense.Amount;
        existingExpense.ExpenseDate = expense.ExpenseDate;
        existingExpense.PaymentStatus = expense.PaymentStatus;
        existingExpense.DocumentNumber = null;
        existingExpense.Description = expense.Description;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Gider kaydi guncellendi.";
        if (existingExpense.ProjectId.HasValue)
        {
            return RedirectToAction("Details", "Projects", new { id = existingExpense.ProjectId.Value });
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        var expense = await _context.Expenses
            .Include(e => e.Project)
            .Include(e => e.Company)
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (expense == null)
        {
            return NotFound();
        }

        return View(expense);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        var expense = await _context.Expenses.FindAsync(id);

        if (expense == null)
        {
            return NotFound();
        }

        var projectId = expense.ProjectId;

        _context.Expenses.Remove(expense);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Gider kaydi silindi.";
        if (projectId.HasValue)
        {
            return RedirectToAction("Details", "Projects", new { id = projectId.Value });
        }

        return RedirectToAction(nameof(Index));
    }

    private void SetCurrentUser(Expense expense)
    {
        var userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            ModelState.AddModelError(string.Empty, "Gider kaydi icin kullanici oturumu bulunamadi.");
            return;
        }

        expense.UserId = userId.Value;
    }

    private static void NormalizeExpense(Expense expense)
    {
        expense.CompanyId = null;
        expense.ExpenseType = "Malzeme";
        expense.PaymentStatus = "Odendi";
        expense.DocumentNumber = null;

        expense.Amount = expense.Quantity * expense.UnitPrice;
    }

    private void ClearDocumentMetadataValidation()
    {
        ModelState.Remove(nameof(Expense.DocumentFilePath));
        ModelState.Remove(nameof(Expense.DocumentOriginalFileName));
        ModelState.Remove(nameof(Expense.DocumentContentType));
    }

    private static bool IsAllowedExpenseDocument(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".webp" };

        return file.Length > 0 && allowedExtensions.Contains(extension);
    }

    private static async Task<(string FilePath, string OriginalFileName, string ContentType)> SaveExpenseDocumentAsync(IFormFile file)
    {
        var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "expense-documents");
        Directory.CreateDirectory(uploadsPath);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var safeFileName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(uploadsPath, safeFileName);

        await using var stream = System.IO.File.Create(physicalPath);
        await file.CopyToAsync(stream);

        return ($"/uploads/expense-documents/{safeFileName}", Path.GetFileName(file.FileName), file.ContentType);
    }

    private bool IsLoggedIn()
    {
        return !string.IsNullOrEmpty(HttpContext.Session.GetString("RoleName"));
    }
}
