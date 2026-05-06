using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfisYonetimSistemi.Models;

namespace OfisYonetimSistemi.Controllers;

public class ExpensesController : Controller
{
    private readonly AppDbContext _context;

    private static readonly string[] ExpenseTypes =
    {
        "Genel",
        "Malzeme",
        "Iscilik",
        "Nakliye",
        "Kira",
        "Fatura",
        "Diger"
    };

    private static readonly string[] PaymentStatuses =
    {
        "Odendi",
        "Bekliyor",
        "Iptal"
    };

    public ExpensesController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        var expenses = await _context.Expenses
            .Include(e => e.Project)
            .Include(e => e.Company)
            .Include(e => e.User)
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync();

        ViewBag.TotalExpenseAmount = expenses.Sum(e => e.Amount);
        ViewBag.PaidExpenseAmount = expenses.Where(e => e.PaymentStatus == "Odendi").Sum(e => e.Amount);
        ViewBag.PendingExpenseAmount = expenses.Where(e => e.PaymentStatus == "Bekliyor").Sum(e => e.Amount);
        ViewBag.ExpenseCount = expenses.Count;

        return View(expenses);
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

        await FillSelectionsAsync(projectId: projectId);
        ViewBag.IsProjectScoped = projectId.HasValue;
        return View(new Expense { ProjectId = projectId, ExpenseDate = DateTime.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Expense expense)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction("Login", "Account");
        }

        ValidateSelections(expense);
        SetCurrentUser(expense);

        if (!ModelState.IsValid)
        {
            await FillSelectionsAsync(expense.ProjectId, expense.CompanyId, expense.ExpenseType, expense.PaymentStatus);
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

        await FillSelectionsAsync(expense.ProjectId, expense.CompanyId, expense.ExpenseType, expense.PaymentStatus);
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

        ValidateSelections(expense);
        SetCurrentUser(expense);

        if (!ModelState.IsValid)
        {
            await FillSelectionsAsync(expense.ProjectId, expense.CompanyId, expense.ExpenseType, expense.PaymentStatus);
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
        existingExpense.ExpenseType = expense.ExpenseType;
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

    private void ValidateSelections(Expense expense)
    {
        if (!ExpenseTypes.Contains(expense.ExpenseType))
        {
            ModelState.AddModelError(nameof(expense.ExpenseType), "Gecerli bir gider turu secin.");
        }

        if (!PaymentStatuses.Contains(expense.PaymentStatus))
        {
            ModelState.AddModelError(nameof(expense.PaymentStatus), "Gecerli bir odeme durumu secin.");
        }
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

    private async Task FillSelectionsAsync(
        int? projectId = null,
        int? companyId = null,
        string? expenseType = null,
        string? paymentStatus = null)
    {
        var projects = await _context.Projects
            .OrderBy(p => p.Name)
            .ToListAsync();

        var companies = await _context.Companies
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.Projects = new SelectList(projects, "Id", "Name", projectId);
        ViewBag.Companies = new SelectList(companies, "Id", "Name", companyId);
        ViewBag.ExpenseTypes = new SelectList(ExpenseTypes, expenseType);
        ViewBag.PaymentStatuses = new SelectList(PaymentStatuses, paymentStatus);
    }

    private bool IsLoggedIn()
    {
        return !string.IsNullOrEmpty(HttpContext.Session.GetString("RoleName"));
    }
}
