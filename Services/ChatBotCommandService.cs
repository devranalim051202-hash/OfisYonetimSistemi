using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OfisYonetimSistemi.Models;
using OfisYonetimSistemi.Models.ViewModels;

namespace OfisYonetimSistemi.Services;

public class ChatBotCommandService
{
    private readonly AppDbContext _context;

    public ChatBotCommandService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ChatCommandResponse> ProcessAsync(string commandText, int userId, string roleName)
    {
        var command = (commandText ?? string.Empty).Trim();
        var normalized = Normalize(command);
        ChatCommandResponse response;

        try
        {
            response = await RouteCommandAsync(command, normalized, userId, roleName);
        }
        catch
        {
            response = Success("SistemHatasi", "Komutunuzu islerken kucuk bir pruz cikti. Lutfen farkli kelimelerle veya daha kisa bir cumle ile tekrar dener misiniz?");
        }

        await SafeLogAsync(userId, command, response);
        return response;
    }

    private async Task<ChatCommandResponse> RouteCommandAsync(string command, string normalized, int userId, string roleName)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return Success("BosKomut", "Lutfen bir sey yazin. Neler yapabildigimi gormek icin 'yardim' yazabilirsiniz.");
        }

        if (IsGreeting(normalized))
        {
            return Success("Selamlama", "Merhaba. Ben Ofis Yonetim Sistemi AI Asistaniyim. Proje, gider, satis, daire, personel ve kar-zarar sorularina rol yetkinize gore cevap verebilirim.");
        }

        if (IsIdentityQuestion(normalized))
        {
            return Introduce(roleName);
        }

        if (IsHelpQuestion(normalized))
        {
            return Help(roleName);
        }

        if (IsPersonnelCommand(normalized))
        {
            return await ListPersonnelAsync(roleName);
        }

        if (IsProjectListCommand(normalized))
        {
            return await ListProjectsAsync(roleName);
        }

        if (IsBuyerListCommand(normalized))
        {
            return await ListAllBuyersAsync(roleName);
        }

        if (IsLowStockCommand(normalized))
        {
            return await ListLowStockMaterialsAsync(roleName);
        }

        if (IsMaterialExpenseSummaryCommand(normalized))
        {
            return await ShowMaterialExpenseSummaryAsync(normalized, roleName);
        }

        if (IsTopExpenseCategoryCommand(normalized))
        {
            return await ShowTopExpenseCategoriesAsync(roleName);
        }

        if (IsProjectInfoCommand(normalized))
        {
            return await ShowProjectInfoAsync(normalized, roleName);
        }

        if (IsProfitLossCommand(normalized))
        {
            return await ShowProfitLossAsync(normalized, roleName);
        }

        if (IsApartmentSalesCommand(normalized))
        {
            var buyerName = await FindBuyerNameInCommandAsync(normalized);
            if (buyerName != null)
            {
                return await ShowSpecificBuyerSalesAsync(buyerName, roleName);
            }
            return await ListApartmentSalesAsync(normalized, roleName);
        }

        if (IsEmptyApartmentCommand(normalized))
        {
            return await ListApartmentsAsync(normalized, roleName, isSold: false);
        }

        if (IsSoldApartmentCommand(normalized))
        {
            return await ListApartmentsAsync(normalized, roleName, isSold: true);
        }

        if (IsSupplierExpenseCommand(normalized))
        {
            var supplierName = await FindSupplierNameInCommandAsync(normalized);
            if (supplierName != null)
            {
                return await ShowSpecificSupplierExpensesAsync(supplierName, roleName);
            }
            return await ShowSupplierExpensesAsync(normalized, roleName);
        }

        if (IsExpenseQueryCommand(normalized))
        {
            return await ListExpensesAsync(normalized, roleName);
        }

        if (IsDashboardSummaryCommand(normalized))
        {
            return await ShowSystemSummaryAsync(roleName);
        }

        if (LooksLikeExpenseCreate(normalized))
        {
            return await CreateExpenseFromCommandAsync(command, normalized, userId, roleName);
        }

        if (IsProjectCreateCommand(normalized))
        {
            return ProjectCreateHelp(roleName);
        }

        return SmartFallback(normalized, roleName);
    }

    private static ChatCommandResponse Introduce(string roleName)
    {
        return Success("KendiniTanitma",
            $"Ben Ofis Yonetim Sistemi AI Asistaniyim. Rolunuz: {roleName}. Yetkiniz neyse sadece o kapsamdaki proje, gider, satis, daire ve personel bilgilerini cevaplarim.");
    }

    private static ChatCommandResponse Help(string roleName)
    {
        var commands = new List<string>
        {
            "- Kendini tanit",
            "- Projeleri listele",
            "- Wan projesi ne durumda",
            "- Bu ayki giderleri goster",
            "- Hangi firmadan ne alinmis",
            "- Sistem ozeti"
        };

        if (CanViewSales(roleName))
        {
            commands.Add("- Yapilan daire satislarini goster");
            commands.Add("- Bos daireleri listele");
            commands.Add("- Satilan daireleri goster");
            commands.Add("- Wan projesinin kar zarar durumunu hesapla");
        }

        if (CanManageExpenses(roleName))
        {
            commands.Add("- Wan projesine bugun 5 cimento geldi tanesi 250 TL");
        }

        if (CanViewPersonnel(roleName))
        {
            commands.Add("- Personeller ne is yapiyor");
        }

        return Success("Yardim", "Kullanabileceginiz komut ornekleri:\n" + string.Join("\n", commands));
    }

    private async Task<ChatCommandResponse> ListProjectsAsync(string roleName)
    {
        if (!CanViewProjects(roleName))
        {
            return Unauthorized("ProjeListeleme");
        }

        var projects = await _context.Projects
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .Take(12)
            .Select(p => new { p.Name, p.Status, p.StartDate, p.EndDate, p.FloorCount, p.ApartmentCount })
            .ToListAsync();

        if (!projects.Any())
        {
            return Success("ProjeListeleme", "Sistemde kayitli proje bulunmuyor.");
        }

        var lines = projects.Select(p =>
            $"- {p.Name} | {p.Status} | {p.StartDate:dd.MM.yyyy} - {(p.EndDate.HasValue ? p.EndDate.Value.ToString("dd.MM.yyyy") : "-")} | {p.FloorCount} kat / {p.ApartmentCount} daire");

        return Success("ProjeListeleme", "Kayitli projeler:\n" + string.Join("\n", lines));
    }

    private async Task<ChatCommandResponse> ShowProjectInfoAsync(string normalizedCommand, string roleName)
    {
        if (!CanViewProjects(roleName)) return Unauthorized("ProjeBilgisi");

        var project = await FindProjectFromCommandAsync(normalizedCommand, fallbackToLatest: true);
        if (project == null)
        {
            return Success("ProjeBilgisi", "Sistemde hic proje bulunamadi.");
        }

        var salesIncome = await _context.ApartmentSales.AsNoTracking().Where(s => s.Apartment != null && s.Apartment.ProjectId == project.Id).SumAsync(s => s.SalePrice);
        var directExpense = await _context.Expenses.AsNoTracking().Where(e => e.ProjectId == project.Id).SumAsync(e => e.Amount);
        var invoiceExpense = await _context.Invoices.AsNoTracking().Where(i => i.ProjectId == project.Id).SumAsync(i => i.TotalAmount);
        var stockExpense = await _context.StockMovements.AsNoTracking().Where(sm => sm.ProjectId == project.Id).SumAsync(sm => sm.TotalPrice);

        var totalExpense = directExpense + invoiceExpense + stockExpense;
        var balance = salesIncome - totalExpense;
        var status = balance >= 0 ? "kar" : "zarar";

        var topSuppliers = await _context.Expenses.AsNoTracking()
            .Where(e => e.ProjectId == project.Id)
            .GroupBy(e => e.SupplierName)
            .Select(g => new { Name = g.Key, Amount = g.Sum(e => e.Amount) })
            .OrderByDescending(g => g.Amount)
            .Take(3)
            .ToListAsync();

        var text = new StringBuilder();
        if (!TextMatches(normalizedCommand, Normalize(project.Name))) 
        {
            text.AppendLine($"(Proje adi belirtilmedigi icin en guncel proje baz alindi)");
        }
        
        text.AppendLine($"{project.Name} Proje Ozeti:");
        text.AppendLine($"- Durum: {project.Status}");
        text.AppendLine($"- Konum: {project.Location ?? "-"}");
        text.AppendLine($"- Gelir (Satislar): {salesIncome:N2} TL");
        text.AppendLine($"- Toplam Gider: {totalExpense:N2} TL");
        text.AppendLine($"- Genel Durum: {Math.Abs(balance):N2} TL {status}");
        
        if (topSuppliers.Any())
        {
            text.AppendLine("\nEn Cok Alim Yapilan Firmalar:");
            foreach (var sup in topSuppliers)
            {
                text.AppendLine($"- {sup.Name} ({sup.Amount:N2} TL)");
            }
        }

        return Success("ProjeBilgisi", text.ToString().Trim());
    }

    private async Task<ChatCommandResponse> ListApartmentSalesAsync(string normalizedCommand, string roleName)
    {
        if (!CanViewSales(roleName))
        {
            return Unauthorized("DaireSatisListeleme");
        }

        var project = await FindProjectFromCommandAsync(normalizedCommand);
        var query = _context.ApartmentSales
            .AsNoTracking()
            .Include(s => s.Apartment)
            .ThenInclude(a => a!.Project)
            .AsQueryable();

        if (project != null)
        {
            query = query.Where(s => s.Apartment != null && s.Apartment.ProjectId == project.Id);
        }

        var sales = await query
            .OrderByDescending(s => s.SaleDate)
            .Take(20)
            .Select(s => new
            {
                ProjectName = s.Apartment != null && s.Apartment.Project != null ? s.Apartment.Project.Name : "-",
                ApartmentNumber = s.Apartment != null ? s.Apartment.ApartmentNumber : "-",
                RoomType = s.Apartment != null ? s.Apartment.RoomType : "-",
                s.BuyerFullName,
                s.SalePrice,
                s.PaymentType,
                s.SaleDate
            })
            .ToListAsync();

        if (!sales.Any())
        {
            return Success("DaireSatisListeleme", project == null ? "Kayitli daire satisi bulunamadi." : $"{project.Name} projesinde kayitli daire satisi bulunamadi.");
        }

        var total = sales.Sum(s => s.SalePrice);
        var lines = sales.Select(s =>
            $"- {s.ProjectName} | Daire: {s.ApartmentNumber} ({s.RoomType}) | Alici: {s.BuyerFullName} | {s.SalePrice:N2} TL | {s.PaymentType} | {s.SaleDate:dd.MM.yyyy}");

        return Success("DaireSatisListeleme", $"Daire satislari toplam: {total:N2} TL\n" + string.Join("\n", lines));
    }

    private async Task<ChatCommandResponse> ShowProfitLossAsync(string normalizedCommand, string roleName)
    {
        if (!CanViewProfitLoss(roleName))
        {
            return Unauthorized("KarZararHesaplama");
        }

        var project = await FindProjectFromCommandAsync(normalizedCommand, fallbackToLatest: true);
        if (project == null)
        {
            return Success("KarZararHesaplama", "Sistemde hic proje bulunamadi.");
        }

        var salesIncome = await _context.ApartmentSales.AsNoTracking()
            .Where(s => s.Apartment != null && s.Apartment.ProjectId == project.Id)
            .SumAsync(s => s.SalePrice);
        var directExpense = await _context.Expenses.AsNoTracking().Where(e => e.ProjectId == project.Id).SumAsync(e => e.Amount);
        var invoiceExpense = await _context.Invoices.AsNoTracking().Where(i => i.ProjectId == project.Id).SumAsync(i => i.TotalAmount);
        var stockExpense = await _context.StockMovements.AsNoTracking().Where(sm => sm.ProjectId == project.Id).SumAsync(sm => sm.TotalPrice);

        var totalExpense = directExpense + invoiceExpense + stockExpense;
        var balance = salesIncome - totalExpense;
        var status = balance >= 0 ? "kar" : "zarar";
        var ratio = salesIncome > 0 ? balance / salesIncome * 100 : 0;

        var text = new StringBuilder();
        text.AppendLine($"{project.Name} kar-zarar ozeti:");
        text.AppendLine($"- Satis geliri: {salesIncome:N2} TL");
        text.AppendLine($"- Direkt gider: {directExpense:N2} TL");
        text.AppendLine($"- Evrak/fatura gideri: {invoiceExpense:N2} TL");
        text.AppendLine($"- Stok gideri: {stockExpense:N2} TL");
        text.AppendLine($"- Toplam gider: {totalExpense:N2} TL");
        text.AppendLine($"- Sonuc: {Math.Abs(balance):N2} TL {status}");
        text.AppendLine(salesIncome > 0 ? $"- Oran: %{ratio:N2}" : "- Satis geliri olmadigi icin oran hesaplanamadi.");

        return Success("KarZararHesaplama", text.ToString().Trim());
    }

    private async Task<ChatCommandResponse> ShowSupplierExpensesAsync(string normalizedCommand, string roleName)
    {
        if (!CanViewExpenses(roleName))
        {
            return Unauthorized("FirmaGiderOzeti");
        }

        var project = await FindProjectFromCommandAsync(normalizedCommand, fallbackToLatest: true);
        var query = _context.Expenses.AsNoTracking().Include(e => e.Project).AsQueryable();
        if (project != null && !ContainsAny(normalizedCommand, "butun projeler", "tum projeler"))
        {
            query = query.Where(e => e.ProjectId == project.Id);
        }

        var grouped = await query
            .GroupBy(e => new { e.SupplierName, ProjectName = e.Project != null ? e.Project.Name : "-" })
            .Select(g => new
            {
                g.Key.SupplierName,
                g.Key.ProjectName,
                ItemCount = g.Count(),
                TotalQuantity = g.Sum(e => e.Quantity),
                TotalAmount = g.Sum(e => e.Amount),
                Items = g.Select(e => e.Title).Distinct().Take(4).ToList()
            })
            .OrderByDescending(g => g.TotalAmount)
            .Take(12)
            .ToListAsync();

        if (!grouped.Any())
        {
            return Success("FirmaGiderOzeti", project == null ? "Firma bazli gider bulunamadi." : $"{project.Name} projesinde firma bazli gider bulunamadi.");
        }

        var title = project == null ? "Firma bazli alinanlar ve giderler:" : $"{project.Name} firma bazli alinanlar ve giderler:";
        var lines = grouped.Select(g =>
            $"- {g.SupplierName} | Proje: {g.ProjectName} | Alinanlar: {string.Join(", ", g.Items)} | Kayit: {g.ItemCount} | Miktar: {g.TotalQuantity:N2} | Gider: {g.TotalAmount:N2} TL");

        return Success("FirmaGiderOzeti", title + "\n" + string.Join("\n", lines));
    }

    private async Task<ChatCommandResponse> ListExpensesAsync(string normalizedCommand, string roleName)
    {
        if (!CanViewExpenses(roleName))
        {
            return Unauthorized("GiderListeleme");
        }

        var project = await FindProjectFromCommandAsync(normalizedCommand);
        var query = _context.Expenses.AsNoTracking().Include(e => e.Project).AsQueryable();

        if (ContainsAny(normalizedCommand, "bu ay", "aylik", "ayki"))
        {
            var today = DateTime.Today;
            query = query.Where(e => e.ExpenseDate.Month == today.Month && e.ExpenseDate.Year == today.Year);
        }

        if (project != null)
        {
            query = query.Where(e => e.ProjectId == project.Id);
        }

        var expenses = await query
            .OrderByDescending(e => e.ExpenseDate)
            .Take(15)
            .Select(e => new
            {
                ProjectName = e.Project != null ? e.Project.Name : "-",
                e.Title,
                e.SupplierName,
                e.Quantity,
                e.UnitPrice,
                e.Amount,
                e.ExpenseDate
            })
            .ToListAsync();

        if (!expenses.Any())
        {
            return Success("GiderListeleme", "Bu kriterlere uygun gider kaydi bulunamadi.");
        }

        var total = expenses.Sum(e => e.Amount);
        var lines = expenses.Select(e =>
            $"- {e.ProjectName} | {e.Title} | {e.SupplierName} | {e.Quantity:N2} x {e.UnitPrice:N2} TL = {e.Amount:N2} TL | {e.ExpenseDate:dd.MM.yyyy}");

        return Success("GiderListeleme", $"Gider toplam: {total:N2} TL\n" + string.Join("\n", lines));
    }

    private async Task<ChatCommandResponse> ShowMaterialExpenseSummaryAsync(string normalizedCommand, string roleName)
    {
        if (!CanViewExpenses(roleName))
        {
            return Unauthorized("MalzemeGiderOzeti");
        }

        var project = await FindProjectFromCommandAsync(normalizedCommand);
        var query = _context.Expenses.AsNoTracking().Include(e => e.Project).AsQueryable();

        if (IsCurrentMonthQuery(normalizedCommand))
        {
            var today = DateTime.Today;
            query = query.Where(e => e.ExpenseDate.Month == today.Month && e.ExpenseDate.Year == today.Year);
        }

        if (project != null && !ContainsAny(normalizedCommand, "butun projeler", "tum projeler", "her projede", "tum projelerde"))
        {
            query = query.Where(e => e.ProjectId == project.Id);
        }

        var expenses = await query
            .OrderByDescending(e => e.ExpenseDate)
            .Select(e => new
            {
                ProjectName = e.Project != null ? e.Project.Name : "-",
                e.Title,
                e.SupplierName,
                e.Quantity,
                e.UnitPrice,
                e.Amount,
                e.ExpenseDate
            })
            .ToListAsync();

        if (!expenses.Any())
        {
            return Success("MalzemeGiderOzeti", "Bu kriterlere uygun malzeme gideri bulunamadi.");
        }

        var materialTitle = await FindMaterialTitleInCommandAsync(normalizedCommand);
        if (!string.IsNullOrWhiteSpace(materialTitle))
        {
            expenses = expenses
                .Where(e => TextMatches(Normalize(e.Title), Normalize(materialTitle)))
                .ToList();

            if (!expenses.Any())
            {
                return Success("MalzemeGiderOzeti", $"{materialTitle} icin kayitli gider bulunamadi.");
            }

            var totalQuantity = expenses.Sum(e => e.Quantity);
            var totalAmount = expenses.Sum(e => e.Amount);
            var averageUnitPrice = totalQuantity > 0 ? totalAmount / totalQuantity : 0;
            var period = IsCurrentMonthQuery(normalizedCommand) ? "Bu ay" : "Toplam";

            var projectLines = expenses
                .GroupBy(e => e.ProjectName)
                .OrderByDescending(g => g.Sum(e => e.Amount))
                .Take(8)
                .Select(g => $"- {g.Key}: {g.Sum(e => e.Quantity):N2} adet | {g.Sum(e => e.Amount):N2} TL");

            return Success(
                "MalzemeGiderOzeti",
                $"{period} {materialTitle} ozeti:\n" +
                $"- Toplam miktar: {totalQuantity:N2}\n" +
                $"- Toplam fiyat: {totalAmount:N2} TL\n" +
                $"- Ortalama birim fiyat: {averageUnitPrice:N2} TL\n" +
                "Proje dagilimi:\n" + string.Join("\n", projectLines));
        }

        var grouped = expenses
            .GroupBy(e => e.Title)
            .Select(g => new
            {
                Title = g.Key,
                TotalQuantity = g.Sum(e => e.Quantity),
                TotalAmount = g.Sum(e => e.Amount),
                AverageUnitPrice = g.Sum(e => e.Quantity) > 0 ? g.Sum(e => e.Amount) / g.Sum(e => e.Quantity) : 0,
                ProjectCount = g.Select(e => e.ProjectName).Distinct().Count()
            })
            .OrderByDescending(g => g.TotalAmount)
            .Take(15)
            .ToList();

        var title = IsCurrentMonthQuery(normalizedCommand)
            ? "Bu ay kullanilan malzemeler ve giderleri:"
            : "Tum projelerde kullanilan malzemeler ve giderleri:";

        var lines = grouped.Select(g =>
            $"- {g.Title}: {g.TotalQuantity:N2} adet | {g.TotalAmount:N2} TL | Ortalama: {g.AverageUnitPrice:N2} TL | Proje: {g.ProjectCount}");

        return Success("MalzemeGiderOzeti", title + "\n" + string.Join("\n", lines));
    }

    private async Task<ChatCommandResponse> ListApartmentsAsync(string normalizedCommand, string roleName, bool isSold)
    {
        if (!CanViewSales(roleName))
        {
            return Unauthorized(isSold ? "SatilanDaireListeleme" : "BosDaireListeleme");
        }

        var project = await FindProjectFromCommandAsync(normalizedCommand);
        var query = _context.Apartments.AsNoTracking().Include(a => a.Project).Where(a => a.IsSold == isSold);
        if (project != null)
        {
            query = query.Where(a => a.ProjectId == project.Id);
        }

        var apartments = await query
            .OrderBy(a => a.Project!.Name)
            .ThenBy(a => a.FloorNumber)
            .ThenBy(a => a.ApartmentNumber)
            .Take(20)
            .Select(a => new
            {
                ProjectName = a.Project != null ? a.Project.Name : "-",
                a.ApartmentNumber,
                a.FloorNumber,
                a.RoomType,
                a.Price
            })
            .ToListAsync();

        var action = isSold ? "SatilanDaireListeleme" : "BosDaireListeleme";
        if (!apartments.Any())
        {
            return Success(action, isSold ? "Satilan daire bulunamadi." : "Bos daire bulunamadi.");
        }

        var title = isSold ? "Satilan daireler:" : "Bos daireler:";
        var lines = apartments.Select(a => $"- {a.ProjectName} | No: {a.ApartmentNumber} | Kat: {a.FloorNumber} | {a.RoomType} | {a.Price:N2} TL");
        return Success(action, title + "\n" + string.Join("\n", lines));
    }

    private async Task<ChatCommandResponse> ListPersonnelAsync(string roleName)
    {
        if (!CanViewPersonnel(roleName))
        {
            return Unauthorized("PersonelListeleme");
        }

        var users = await _context.Users.AsNoTracking()
            .Include(u => u.Role)
            .OrderBy(u => u.Role!.Name)
            .ThenBy(u => u.FullName)
            .Select(u => new
            {
                u.FullName,
                RoleName = u.Role != null ? u.Role.Name : "Personel",
                RoleDescription = u.Role != null ? u.Role.Description : null,
                u.Email,
                ExpenseCount = u.Expenses.Count
            })
            .Take(20)
            .ToListAsync();

        if (!users.Any())
        {
            return Success("PersonelListeleme", "Kayitli personel bulunamadi.");
        }

        var lines = users.Select(u =>
            $"- {u.FullName} | Gorev: {u.RoleName} | Is tanimi: {(u.RoleDescription ?? RoleDescription(u.RoleName))} | E-posta: {u.Email} | Gider kaydi: {u.ExpenseCount}");

        return Success("PersonelListeleme", "Personel gorev ozeti:\n" + string.Join("\n", lines));
    }

    private async Task<ChatCommandResponse> ShowSystemSummaryAsync(string roleName)
    {
        if (!CanViewProjects(roleName))
        {
            return Unauthorized("SistemOzeti");
        }

        var projectCount = await _context.Projects.AsNoTracking().CountAsync();
        var expenseTotal = CanViewExpenses(roleName) ? await _context.Expenses.AsNoTracking().SumAsync(e => e.Amount) : 0;
        var soldCount = CanViewSales(roleName) ? await _context.Apartments.AsNoTracking().CountAsync(a => a.IsSold) : 0;
        var emptyCount = CanViewSales(roleName) ? await _context.Apartments.AsNoTracking().CountAsync(a => !a.IsSold) : 0;

        var text = new StringBuilder();
        text.AppendLine("Sistem ozeti:");
        text.AppendLine($"- Proje sayisi: {projectCount}");
        if (CanViewExpenses(roleName))
        {
            text.AppendLine($"- Toplam gider: {expenseTotal:N2} TL");
        }
        if (CanViewSales(roleName))
        {
            text.AppendLine($"- Satilan daire: {soldCount}");
            text.AppendLine($"- Bos daire: {emptyCount}");
        }

        return Success("SistemOzeti", text.ToString().Trim());
    }

    private async Task<ChatCommandResponse> CreateExpenseFromCommandAsync(string command, string normalizedCommand, int userId, string roleName)
    {
        if (!CanManageExpenses(roleName))
        {
            return Unauthorized("GiderEkleme");
        }

        var expenseMatch = Regex.Match(
            normalizedCommand,
            @"(?<project>.+?)\s*(projesi|projesine|proje)?\s*(bugun)?\s*(?<quantity>\d+([,.]\d+)?)\s+(?<title>[a-z0-9\s]+?)\s+(geldi|alindi|eklendi).*?(tanesi|birim|birim fiyati)\s*(?<unitPrice>\d+([,.]\d+)?)",
            RegexOptions.IgnoreCase);

        if (!expenseMatch.Success)
        {
            return Success("GiderEkleme", "Gider ekleme komutunuzu tam olarak anlayamadim. Lutfen su formata uygun yazmayi deneyin: 'wan projesine bugun 5 cimento alindi tanesi 250 TL'.");
        }

        var projectQuery = expenseMatch.Groups["project"].Value.Replace("'", string.Empty).Trim();
        var project = await FindProjectFromCommandAsync(projectQuery);
        if (project == null)
        {
            return Success("GiderEkleme", "Belirttiginiz projeyi sistemde bulamadim. Lutfen proje adini dogru yazdiginizdan emin olun.");
        }

        var quantity = ParseDecimal(expenseMatch.Groups["quantity"].Value);
        var unitPrice = ParseDecimal(expenseMatch.Groups["unitPrice"].Value);
        var title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(expenseMatch.Groups["title"].Value.Trim());

        var expense = new Expense
        {
            ProjectId = project.Id,
            UserId = userId,
            Title = title,
            SupplierName = "Chatbot komutu",
            ExpenseType = "Malzeme",
            Quantity = quantity,
            UnitPrice = unitPrice,
            Amount = quantity * unitPrice,
            ExpenseDate = DateTime.Today,
            PaymentStatus = "Odendi",
            Description = command,
            DocumentFilePath = "Chatbot uzerinden belge eklenmedi",
            DocumentOriginalFileName = "Chatbot komutu",
            CreatedAt = DateTime.Now
        };

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync();
        return Success("GiderEkleme", $"{project.Name} projesine {quantity:N2} {title} gideri eklendi. Toplam: {expense.Amount:N2} TL.");
    }

    private static ChatCommandResponse ProjectCreateHelp(string roleName)
    {
        return CanManageProjects(roleName)
            ? Success("ProjeEkleme", "Yeni proje ekleyebilirsiniz. Proje ekleme ekrani: /Projects/Create")
            : Unauthorized("ProjeEkleme");
    }

    private ChatCommandResponse SmartFallback(string normalized, string roleName)
    {
        if (HasPersonnelIntent(normalized))
        {
            return CanViewPersonnel(roleName)
                ? Success("Oneri", "Personel ile ilgili sordugunuzu anladim. Personel listesini ve gorevlerini gormek icin 'personeller ne is yapiyor' veya 'calisan listesi' yazabilirsiniz.")
                : Unauthorized("PersonelListeleme");
        }

        if (ContainsAny(normalized, "proje", "insaat", "santiye"))
        {
            return Success("Oneri", "Proje ile ilgili bir sey sordugunuzu anliyorum ancak tam olarak ne istediginizi cikaramadim. Sunlari deneyebilirsiniz: 'projeleri listele', 'wan projesi ne durumda', 'wan kar zarar'.");
        }
        if (ContainsAny(normalized, "gider", "masraf", "harcama", "firma", "tedarikci", "malzeme", "fatura", "odeme"))
        {
            return Success("Oneri", "Giderler veya masraflarla ilgili bir sorunuz var sanirim. Daha net sonuc icin sunlari deneyebilirsiniz: 'bu ayki giderleri goster' veya 'hangi firmadan ne alinmis'.");
        }
        if (ContainsAny(normalized, "daire", "satis", "musteri", "alici", "ev"))
        {
            return CanViewSales(roleName)
                ? Success("Oneri", "Daire veya satislarla ilgili bir bilgi ariyorsunuz. Ornegin sunlari yazabilirsiniz: 'yapilan daire satislari', 'bos daireleri listele', 'satilan daireleri goster'.")
                : Unauthorized("SatisBilgisi");
        }
        if (ContainsAny(normalized, "personel", "calisan", "gorev", "kim", "eleman", "isci", "ekip"))
        {
            return CanViewPersonnel(roleName)
                ? Success("Oneri", "Personel ile ilgili sordugunuzu anladim. Ornegin 'personeller ne is yapiyor' veya 'calisan listesi' yazabilirsiniz.")
                : Unauthorized("PersonelListeleme");
        }

        return Success("Bilinmeyen", "Komutunuzu tam netlestiremedim ama ana kelimelerden anlamaya calisiyorum. Proje, gider, firma, satis, daire, personel, kar-zarar veya sistem ozeti gibi konularda soruyu biraz daha acik yazabilirsiniz.");
    }

    private async Task<ChatCommandResponse> ShowSpecificSupplierExpensesAsync(string supplierName, string roleName)
    {
        if (!CanViewExpenses(roleName)) return Unauthorized("FirmaGiderDetayi");

        var expenses = await _context.Expenses.AsNoTracking()
            .Where(e => e.SupplierName == supplierName)
            .OrderByDescending(e => e.ExpenseDate)
            .Take(15)
            .ToListAsync();

        if (!expenses.Any())
        {
            return Success("FirmaGiderDetayi", $"{supplierName} firmasina ait herhangi bir kayit bulunamadi.");
        }

        var total = expenses.Sum(e => e.Amount);
        var lines = expenses.Select(e => $"- {e.Title} | {e.Quantity:N2} adet | {e.Amount:N2} TL | {e.ExpenseDate:dd.MM.yyyy}");

        return Success("FirmaGiderDetayi", $"{supplierName} firmasindan alinanlar (Toplam {total:N2} TL):\n" + string.Join("\n", lines));
    }

    private async Task<ChatCommandResponse> ShowSpecificBuyerSalesAsync(string buyerName, string roleName)
    {
        if (!CanViewSales(roleName)) return Unauthorized("MusteriDetayi");

        var sales = await _context.ApartmentSales.AsNoTracking()
            .Include(s => s.Apartment)
            .ThenInclude(a => a!.Project)
            .Where(s => s.BuyerFullName == buyerName)
            .ToListAsync();

        if (!sales.Any())
        {
            return Success("MusteriDetayi", $"{buyerName} adli kisiye yapilmis bir satis bulunamadi.");
        }

        var total = sales.Sum(s => s.SalePrice);
        var lines = sales.Select(s => $"- {s.Apartment?.Project?.Name ?? "-"} | Daire: {s.Apartment?.ApartmentNumber} | {s.SalePrice:N2} TL | {s.SaleDate:dd.MM.yyyy}");

        return Success("MusteriDetayi", $"{buyerName} adli kisiye yapilan satislar (Toplam {total:N2} TL):\n" + string.Join("\n", lines));
    }

    private async Task<ChatCommandResponse> ListAllBuyersAsync(string roleName)
    {
        if (!CanViewSales(roleName)) return Unauthorized("MusteriListesi");

        var buyers = await _context.ApartmentSales.AsNoTracking()
            .OrderByDescending(s => s.SaleDate)
            .Select(s => new { s.BuyerFullName, s.SaleDate })
            .Take(30)
            .ToListAsync();

        if (!buyers.Any())
        {
            return Success("MusteriListesi", "Henuz hic daire satisi yapilmamis.");
        }

        var names = buyers.OrderByDescending(b => b.SaleDate).Select(b => b.BuyerFullName).Distinct().ToList();
        return Success("MusteriListesi", "Daire satilan kisiler (Son satislar):\n- " + string.Join("\n- ", names));
    }

    private async Task<ChatCommandResponse> ListLowStockMaterialsAsync(string roleName)
    {
        if (!CanViewExpenses(roleName)) return Unauthorized("StokDurumu");

        var materials = await _context.Materials.AsNoTracking()
            .Where(m => m.CurrentStock <= m.MinimumStockLevel)
            .OrderBy(m => m.CurrentStock)
            .ToListAsync();

        if (!materials.Any())
        {
            return Success("StokDurumu", "Kritik seviyede malzeme stogu bulunmuyor. Tum stoklar yeterli.");
        }

        var lines = materials.Select(m => $"- {m.Name} | Mevcut: {m.CurrentStock:N2} {m.Unit} | Kritik: {m.MinimumStockLevel:N2}");
        return Success("StokDurumu", "Stogu azalan kritik malzemeler:\n" + string.Join("\n", lines));
    }

    private async Task<ChatCommandResponse> ShowTopExpenseCategoriesAsync(string roleName)
    {
        if (!CanViewExpenses(roleName)) return Unauthorized("GiderAnalizi");

        var groups = await _context.Expenses.AsNoTracking()
            .GroupBy(e => e.Title)
            .Select(g => new { Title = g.Key, Total = g.Sum(e => e.Amount) })
            .OrderByDescending(g => g.Total)
            .Take(5)
            .ToListAsync();

        if (!groups.Any()) return Success("GiderAnalizi", "Kayitli gider bulunmuyor.");

        var lines = groups.Select(g => $"- {g.Title}: {g.Total:N2} TL");
        return Success("GiderAnalizi", "En yuksek gider kalemleri:\n" + string.Join("\n", lines));
    }

    private async Task<string?> FindSupplierNameInCommandAsync(string normalizedCommand)
    {
        var suppliers = await _context.Expenses.AsNoTracking()
            .Select(e => e.SupplierName)
            .Distinct()
            .ToListAsync();

        return suppliers.FirstOrDefault(s => !string.IsNullOrEmpty(s) && TextMatches(normalizedCommand, Normalize(s)));
    }

    private async Task<string?> FindBuyerNameInCommandAsync(string normalizedCommand)
    {
        var buyers = await _context.ApartmentSales.AsNoTracking()
            .Select(s => s.BuyerFullName)
            .Distinct()
            .ToListAsync();

        return buyers.FirstOrDefault(b => !string.IsNullOrEmpty(b) && TextMatches(normalizedCommand, Normalize(b)));
    }

    private async Task<string?> FindMaterialTitleInCommandAsync(string normalizedCommand)
    {
        var titles = await _context.Expenses.AsNoTracking()
            .Select(e => e.Title)
            .Distinct()
            .ToListAsync();

        return titles
            .Where(title => !string.IsNullOrWhiteSpace(title))
            .OrderByDescending(title => title.Length)
            .FirstOrDefault(title => TextMatches(normalizedCommand, Normalize(title)));
    }

    private async Task SafeLogAsync(int userId, string commandText, ChatCommandResponse response)
    {
        try
        {
            _context.ChatBotLogs.Add(new ChatBotLog
            {
                UserId = userId,
                CommandText = string.IsNullOrWhiteSpace(commandText) ? "(bos komut)" : commandText,
                DetectedAction = response.Action,
                ResponseText = response.ResponseText.Length > 2000 ? response.ResponseText[..2000] : response.ResponseText,
                IsSuccessful = response.IsSuccessful,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }
        catch
        {
            // Chatbot cevabi log hatasi yuzunden kullaniciya hata vermemeli.
        }
    }

    private static bool IsGreeting(string value) => IsExactAny(value, "merhaba", "selam", "sa", "gunaydin", "iyi gunler", "iyi aksamlar", "naber", "merhabalar", "nasilsin");
    private static bool IsIdentityQuestion(string value) => ContainsAll(value, "kendini", "tanit") || ContainsAny(value, "sen kimsin", "nesin", "ne yaparsin", "kendinden bahset", "gorevin ne", "kimsiniz");
    private static bool IsHelpQuestion(string value) => ContainsAny(value, "yardim", "komutlar", "neler yapabilirsin", "ne yapabilirsin", "ornek komut", "nasil kullanilir", "neler sorabilirim");
    private static bool IsProjectListCommand(string value) => ContainsAll(value, "proje", "liste") || ContainsAll(value, "proje", "goster") || ContainsAny(value, "projeleri getir", "proje sorgula", "tum projeler", "hangi projeler var", "projeleri sirala", "mevcut projeler", "projelerimiz neler", "butun projeler");
    private static bool IsProjectInfoCommand(string value) => HasProjectIntent(value) && (ContainsAny(value, "ne durumda", "proje bilgisi", "proje ozeti", "detay", "hakkinda bilgi", "nasil gidiyor", "son durum", "bilgi ver") || ContainsAll(value, "proje", "durum") || ContainsAny(value, "projem nasil", "projemiz ne alemde"));
    private static bool IsApartmentSalesCommand(string value) =>
        HasApartmentIntent(value) && HasSalesIntent(value) &&
        (ContainsAny(value, "ne durumda", "goster", "liste", "rapor", "kimlere", "kimle", "kime", "kimler", "nasil", "durum", "sorgula", "getir", "satildi", "sattim", "sattik") ||
         ContainsAny(value, "yapilan daire satis", "daire satislarini", "satislari goster", "satis raporu", "satilan daire satis", "ne kadar ev satildi", "satislari getir", "satis listesi", "satislar nasil"));
    private static bool IsProfitLossCommand(string value) => ContainsAny(value, "kar zarar", "kar/zarar", "karlilik", "zarar durumu", "kazanc nedir", "ne kadar kazandik", "kar mi zarar mi", "gelir gider dengesi", "karimiz ne", "maliyet") || ContainsAll(value, "satis", "gider", "fark") || ContainsAll(value, "gelir", "gider", "hesapla");
    private static bool IsSupplierExpenseCommand(string value) => ContainsAny(value, "hangi firmadan ne alinmis", "firmadan ne alinmis", "firma gider", "tedarikci gider", "nerde gider olmus", "nerede gider olmus", "kimden ne aldik", "hangi tedarikci", "firma listesi", "firmalara ne odedik", "sirketlere odenen", "kimlere para gitti", "alinan ne", "hangi malzemeyi aldik", "hangi malzemeleri", "hangi firma", "nerden alindi", "nereden aldik") || ContainsAll(value, "firma", "gider") || ContainsAll(value, "tedarikci", "alinan");
    private static bool IsPersonnelCommand(string value) => HasPersonnelIntent(value) && ContainsAny(value, "ne is yapiyor", "gorev", "goster", "liste", "kimler", "calisiyor", "durum", "eleman", "ekip", "kadro", "is bolumu", "rol");
    private static bool IsEmptyApartmentCommand(string value) => ContainsAll(value, "bos", "daire") || ContainsAll(value, "satilmayan", "daire") || ContainsAny(value, "uygun daire", "musait daire", "satilmamis ev", "bos ev", "elde kalan", "satilik ev");
    private static bool IsSoldApartmentCommand(string value) => !HasSalesQuestionIntent(value) && (ContainsAll(value, "satilan", "daire") || ContainsAll(value, "satilmis", "daire") || ContainsAny(value, "satilmis ev", "sahipli daire", "satisi biten", "satilan ev"));
    private static bool IsExpenseQueryCommand(string value) => ContainsAll(value, "gider", "goster") || ContainsAll(value, "gider", "liste") || ContainsAny(value, "gider sorgula", "bu ayki gider", "aylik gider", "harcamalari goster", "masraflari goster", "ne harcadik", "toplam masraf", "gider raporu", "para nereye gitti", "ne kadar giderimiz", "masraflari sirala");
    private static bool IsMaterialExpenseSummaryCommand(string value) =>
        ContainsAny(value, "malzeme", "malzemeleri", "malzemeler", "kullandim", "kullandigim", "kullandik", "aldim", "aldik", "alinan", "toplam ne kadar", "miktar", "adet") &&
        ContainsAny(value, "gider", "fiyat", "tutar", "toplam", "ne kadar", "goster", "liste", "kullandim", "aldim", "aldik");
    private static bool IsDashboardSummaryCommand(string value) => ContainsAny(value, "sistem ozeti", "genel ozet", "dashboard", "durum ozeti", "durum nedir", "genel durum", "ozet gec", "neler yapiyoruz", "isler nasil", "kisa ozet", "bana ozet ver");
    private static bool IsProjectCreateCommand(string value) => ContainsAll(value, "proje", "ekle") || ContainsAll(value, "yeni", "proje");
    private static bool LooksLikeExpenseCreate(string value) => ContainsAny(value, "gider ekle", "geldi", "alindi", "eklendi", "aldik", "gelmis", "dokuldu") && ContainsAny(value, "tl", "lira", "tanesi", "birim", "fiyati", "adet");
    private static bool IsBuyerListCommand(string value) => HasApartmentIntent(value) && ContainsAny(value, "kimlere", "kimle", "kime", "kimler", "musteri", "alici", "alan kisiler", "alanlar kimler", "daire alanlar", "sattim", "sattik");
    private static bool IsLowStockCommand(string value) => ContainsAny(value, "stok durumu", "azalan malzemeler", "kritik stok", "neyimiz bitti", "ne bitti", "malzeme durumu", "stokta ne var");
    private static bool IsTopExpenseCategoryCommand(string value) => ContainsAny(value, "en cok gider", "en yuksek harcama", "nereye para harcadik", "en fazla masraf", "gider kalemleri");

    private static bool HasProjectIntent(string value) => ContainsAny(value, "proje", "projem", "projemiz", "insaat", "santiye");
    private static bool HasPersonnelIntent(string value) => ContainsAny(value, "personel", "personeller", "personler", "calisan", "calisanlar", "eleman", "elemanlar", "ekip", "kadro");
    private static bool HasApartmentIntent(string value) => ContainsAny(value, "daire", "daireler", "daireleri", "ev", "evler", "konut");
    private static bool HasSalesIntent(string value) => ContainsAny(value, "satis", "satislar", "satislari", "satilan", "satildi", "satilmis", "sattim", "sattik", "satildi", "alici", "musteri");
    private static bool HasSalesQuestionIntent(string value) => HasSalesIntent(value) && ContainsAny(value, "ne durumda", "kimlere", "kime", "kimler", "satislari", "satislar", "rapor", "liste", "goster", "durum");
    private static bool IsCurrentMonthQuery(string value) => ContainsAny(value, "bu ay", "aylik", "ayki", "bu ayki");

    private async Task<Project?> FindProjectFromCommandAsync(string normalizedCommand, bool fallbackToLatest = false)
    {
        var projects = await _context.Projects.AsNoTracking().OrderByDescending(p => p.Name.Length).ToListAsync();
        foreach (var project in projects)
        {
            var name = Normalize(project.Name);
            if (TextMatches(normalizedCommand, name))
            {
                return project;
            }
        }

        var tokens = normalizedCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 2 && !ProjectStopWords.Contains(t))
            .ToList();

        var bestMatch = projects
            .Select(p => new { Project = p, Score = tokens.Count(t => TextMatches(Normalize(p.Name), t)) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Project)
            .FirstOrDefault();

        if (bestMatch != null) return bestMatch;

        if (fallbackToLatest && projects.Any())
        {
            return projects.OrderByDescending(p => p.CreatedAt).First();
        }

        return null;
    }

    private static readonly HashSet<string> ProjectStopWords = new()
    {
        "proje", "projesi", "projesinin", "projesinde", "goster", "listele", "hesapla", "durumu", "kar", "zarar", "gider", "satis"
    };

    private static string RoleDescription(string roleName)
    {
        return roleName switch
        {
            "Admin" => "Sistem yonetimi, proje, personel ve tum kayitlar",
            "Mudur" => "Personel ve operasyon kontrolu",
            "Muhasebeci" => "Gider, evrak ve rapor takibi",
            "Sekreter" => "Gunluk gider ve evrak girisi",
            _ => "Sinirli goruntuleme ve takip"
        };
    }

    private static decimal ParseDecimal(string value) => decimal.Parse(value.Replace(',', '.'), CultureInfo.InvariantCulture);

    private static string Normalize(string value)
    {
        var lower = (value ?? string.Empty).Trim().ToLower(new CultureInfo("tr-TR"));
        var builder = new StringBuilder(lower.Length);
        foreach (var ch in lower)
        {
            builder.Append(ch switch
            {
                '\u00e7' => 'c',
                '\u011f' => 'g',
                '\u0131' => 'i',
                '\u00f6' => 'o',
                '\u015f' => 's',
                '\u00fc' => 'u',
                _ => ch
            });
        }

        var normalized = Regex.Replace(builder.ToString(), @"[^\w\s,.']", " ");
        return Regex.Replace(normalized, @"\s+", " ").Trim();
    }

    private static bool ContainsAny(string value, params string[] needles) => needles.Any(needle => TextMatches(value, needle));
    private static bool ContainsAll(string value, params string[] needles) => needles.All(needle => TextMatches(value, needle));
    private static bool IsExactAny(string value, params string[] options) => options.Any(option => string.Equals(Normalize(value), Normalize(option), StringComparison.OrdinalIgnoreCase));

    private static bool TextMatches(string value, string query)
    {
        value = Normalize(value);
        query = Normalize(query);

        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        if (query.Length >= 3 && value.Contains(query))
        {
            return true;
        }

        var valueTokens = GetSearchTokens(value);
        var queryTokens = GetSearchTokens(query);
        if (!queryTokens.Any())
        {
            return false;
        }

        return queryTokens.All(queryToken => valueTokens.Any(valueToken => IsSimilarToken(valueToken, queryToken)));
    }

    private static List<string> GetSearchTokens(string value)
    {
        return Normalize(value)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(token => token.Length > 1 && !KeywordStopWords.Contains(token))
            .Distinct()
            .ToList();
    }

    private static bool IsSimilarToken(string left, string right)
    {
        if (left == right || (right.Length >= 3 && left.Contains(right)) || (left.Length >= 3 && right.Contains(left)))
        {
            return true;
        }

        if (left.Length < 4 || right.Length < 4)
        {
            return false;
        }

        var distance = LevenshteinDistance(left, right);
        var maxLength = Math.Max(left.Length, right.Length);
        var tolerance = maxLength <= 5 ? 1 : 2;

        return distance <= tolerance;
    }

    private static int LevenshteinDistance(string left, string right)
    {
        var costs = new int[right.Length + 1];
        for (var j = 0; j <= right.Length; j++)
        {
            costs[j] = j;
        }

        for (var i = 1; i <= left.Length; i++)
        {
            var previousDiagonal = costs[0];
            costs[0] = i;

            for (var j = 1; j <= right.Length; j++)
            {
                var previousCost = costs[j];
                var substitutionCost = left[i - 1] == right[j - 1] ? previousDiagonal : previousDiagonal + 1;
                costs[j] = Math.Min(Math.Min(costs[j] + 1, costs[j - 1] + 1), substitutionCost);
                previousDiagonal = previousCost;
            }
        }

        return costs[right.Length];
    }

    private static readonly HashSet<string> KeywordStopWords = new()
    {
        "ve", "veya", "ile", "bir", "bu", "su", "o", "ne", "mi", "mu", "mı", "mü", "de", "da", "ki", "icin", "gibi",
        "bana", "ben", "sen", "sana", "lutfen", "acaba", "olan", "olarak", "var", "yok"
    };

    private static ChatCommandResponse Unauthorized(string action) => new()
    {
        Action = action,
        ResponseText = "Bu islemi yapma yetkiniz bulunmamaktadir.",
        IsSuccessful = false
    };

    private static ChatCommandResponse Success(string action, string responseText) => new()
    {
        Action = action,
        ResponseText = responseText,
        IsSuccessful = true
    };

    private static ChatCommandResponse Fail(string action, string responseText) => new()
    {
        Action = action,
        ResponseText = responseText,
        IsSuccessful = false
    };

    private static bool CanViewProjects(string roleName) => IsAny(roleName, "Admin", "Mudur", "Muhasebeci", "Sekreter", "Personel");
    private static bool CanManageProjects(string roleName) => IsAny(roleName, "Admin", "Mudur");
    private static bool CanViewExpenses(string roleName) => IsAny(roleName, "Admin", "Mudur", "Muhasebeci", "Sekreter");
    private static bool CanManageExpenses(string roleName) => IsAny(roleName, "Admin", "Mudur", "Muhasebeci", "Sekreter");
    private static bool CanViewSales(string roleName) => IsAny(roleName, "Admin", "Mudur");
    private static bool CanViewProfitLoss(string roleName) => IsAny(roleName, "Admin", "Mudur", "Muhasebeci");
    private static bool CanViewPersonnel(string roleName) => IsAny(roleName, "Admin", "Mudur");
    private static bool IsAny(string roleName, params string[] roles) => roles.Any(role => string.Equals(roleName, role, StringComparison.OrdinalIgnoreCase));
}
