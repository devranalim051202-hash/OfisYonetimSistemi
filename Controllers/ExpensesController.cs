using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfisYonetimSistemi.Models;

namespace OfisYonetimSistemi.Controllers;

public class ExpensesController : Controller
{
    private readonly AppDbContext _context;

    public ExpensesController(AppDbContext context)
    {
        _context = context;
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

        if (!expense.ProjectId.HasValue)
        {
            ModelState.AddModelError(nameof(expense.ProjectId), "Gider kaydi bir projeye bagli olmalidir.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Project = expense.ProjectId.HasValue
                ? await _context.Projects.FindAsync(expense.ProjectId.Value)
                : null;
            ViewBag.IsProjectScoped = expense.ProjectId.HasValue;
            return View(expense);
        }

        expense.CreatedAt = DateTime.Now;

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync();

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

        if (!ModelState.IsValid)
        {
            ViewBag.IsProjectScoped = expense.ProjectId.HasValue;
            ViewBag.Project = expense.ProjectId.HasValue
                ? await _context.Projects.FindAsync(expense.ProjectId.Value)
                : null;
            return View(expense);
        }

        var existingExpense = await _context.Expenses.FindAsync(id);

        if (existingExpense == null)
        {
            return NotFound();
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
        existingExpense.DocumentNumber = expense.DocumentNumber;
        existingExpense.Description = expense.Description;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Gider kaydi guncellendi.";
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

        _context.Expenses.Remove(expense);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Gider kaydi silindi.";
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

        expense.Amount = expense.Quantity * expense.UnitPrice;
    }

    private bool IsLoggedIn()
    {
        return !string.IsNullOrEmpty(HttpContext.Session.GetString("RoleName"));
    }
}
