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
            response = Fail("SistemHatasi", "Komutu islerken beklenmeyen bir durum olustu. Komutu daha kisa ve net yazarak tekrar deneyin.");
        }

        await SafeLogAsync(userId, command, response);
        return response;
    }

    private async Task<ChatCommandResponse> RouteCommandAsync(string command, string normalized, int userId, string roleName)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return Fail("BosKomut", "Lutfen bir komut yazin. Ornek: 'projeleri listele' veya 'wan kar zarar'.");
        }

        if (IsProjectListCommand(normalized))
        {
            return await ListProjectsAsync(roleName);
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
            return await ShowSupplierExpensesAsync(normalized, roleName);
        }

        if (IsExpenseQueryCommand(normalized))
        {
            return await ListExpensesAsync(normalized, roleName);
        }

        if (IsPersonnelCommand(normalized))
        {
            return await ListPersonnelAsync(roleName);
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
        if (!CanViewProjects(roleName))
        {
            return Unauthorized("ProjeBilgisi");
        }

        var project = await FindProjectFromCommandAsync(normalizedCommand);
        if (project == null)
        {
            return Fail("ProjeBilgisi", "Hangi proje hakkinda bilgi istediginizi anlayamadim. Ornek: 'wan projesi ne durumda'.");
        }

        var expenseTotal = await _context.Expenses.AsNoTracking().Where(e => e.ProjectId == project.Id).SumAsync(e => e.Amount);
        var soldCount = await _context.Apartments.AsNoTracking().CountAsync(a => a.ProjectId == project.Id && a.IsSold);
        var emptyCount = await _context.Apartments.AsNoTracking().CountAsync(a => a.ProjectId == project.Id && !a.IsSold);

        var text = new StringBuilder();
        text.AppendLine($"{project.Name} proje ozeti:");
        text.AppendLine($"- Durum: {project.Status}");
        text.AppendLine($"- Konum: {project.Location ?? "-"}");
        text.AppendLine($"- Baslangic: {project.StartDate:dd.MM.yyyy}");
        text.AppendLine($"- Bitis: {(project.EndDate.HasValue ? project.EndDate.Value.ToString("dd.MM.yyyy") : "-")}");
        text.AppendLine($"- Kat / daire: {project.FloorCount} kat / {project.ApartmentCount} daire");
        text.AppendLine($"- Satilan / bos daire: {soldCount} / {emptyCount}");
        text.AppendLine($"- Kayitli direkt gider: {expenseTotal:N2} TL");

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

        var project = await FindProjectFromCommandAsync(normalizedCommand);
        if (project == null)
        {
            return Fail("KarZararHesaplama", "Kar-zarar icin proje adini yazin. Ornek: 'wan kar zarar' veya 'wan projesinin gelir gider farki'.");
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

        var project = await FindProjectFromCommandAsync(normalizedCommand);
        var query = _context.Expenses.AsNoTracking().Include(e => e.Project).AsQueryable();
        if (project != null)
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
            return Fail("GiderEkleme", "Gider ekleme komutunu tam anlayamadim. Ornek: 'wan projesine bugun 5 cimento geldi tanesi 250 TL'.");
        }

        var projectQuery = expenseMatch.Groups["project"].Value.Replace("'", string.Empty).Trim();
        var project = await FindProjectFromCommandAsync(projectQuery);
        if (project == null)
        {
            return Fail("GiderEkleme", "Belirtilen projeyi bulamadim. Once proje adini kontrol edin.");
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
        if (ContainsAny(normalized, "proje", "insaat", "santiye"))
        {
            return Fail("Oneri", "Proje ile ilgili sordugunuzu anladim. Sunlari deneyin: 'projeleri listele', 'wan projesi ne durumda', 'wan kar zarar'.");
        }
        if (ContainsAny(normalized, "gider", "masraf", "harcama", "firma", "tedarikci", "malzeme"))
        {
            return Fail("Oneri", "Gider veya firma ile ilgili sordugunuzu anladim. Sunlari deneyin: 'bu ayki giderleri goster' veya 'hangi firmadan ne alinmis'.");
        }
        if (ContainsAny(normalized, "daire", "satis", "musteri", "alici"))
        {
            return CanViewSales(roleName)
                ? Fail("Oneri", "Daire veya satis ile ilgili sordugunuzu anladim. Sunlari deneyin: 'yapilan daire satislari', 'bos daireleri listele', 'satilan daireleri goster'.")
                : Unauthorized("SatisBilgisi");
        }
        if (ContainsAny(normalized, "personel", "calisan", "gorev", "kim"))
        {
            return CanViewPersonnel(roleName)
                ? Fail("Oneri", "Personel ile ilgili sordugunuzu anladim. 'personeller ne is yapiyor' yazabilirsiniz.")
                : Unauthorized("PersonelListeleme");
        }

        return Fail("Bilinmeyen", "Bunu tam anlayamadim ama hata yok. 'yardim' yazarsan kullanabilecegin komutlari gosteririm.");
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

    private static bool IsGreeting(string value) => IsExactAny(value, "merhaba", "selam", "sa", "gunaydin", "iyi gunler", "iyi aksamlar");
    private static bool IsIdentityQuestion(string value) => ContainsAll(value, "kendini", "tanit") || ContainsAny(value, "sen kimsin", "nesin", "ne yaparsin", "kendinden bahset");
    private static bool IsHelpQuestion(string value) => ContainsAny(value, "yardim", "komutlar", "neler yapabilirsin", "ne yapabilirsin", "ornek komut");
    private static bool IsProjectListCommand(string value) => ContainsAll(value, "proje", "liste") || ContainsAll(value, "proje", "goster") || ContainsAny(value, "projeleri getir", "proje sorgula", "tum projeler");
    private static bool IsProjectInfoCommand(string value) => ContainsAny(value, "ne durumda", "proje bilgisi", "proje ozeti", "detay") && ContainsAny(value, "proje", "projesi");
    private static bool IsApartmentSalesCommand(string value) => ContainsAny(value, "yapilan daire satis", "daire satislarini", "satislari goster", "satis raporu", "satilan daire satis") || ContainsAll(value, "daire", "satis", "liste");
    private static bool IsProfitLossCommand(string value) => ContainsAny(value, "kar zarar", "kar/zarar", "karlilik", "zarar durumu") || ContainsAll(value, "satis", "gider", "fark") || ContainsAll(value, "gelir", "gider", "hesapla");
    private static bool IsSupplierExpenseCommand(string value) => ContainsAny(value, "hangi firmadan ne alinmis", "firmadan ne alinmis", "firma gider", "tedarikci gider", "nerde gider olmus", "nerede gider olmus") || ContainsAll(value, "firma", "gider") || ContainsAll(value, "tedarikci", "alinan");
    private static bool IsPersonnelCommand(string value) => ContainsAny(value, "personeller ne is yapiyor", "personel gorev", "personelleri goster", "kim ne is yapiyor", "calisanlar ne is yapiyor", "personel listesi") || ContainsAll(value, "personel", "liste");
    private static bool IsEmptyApartmentCommand(string value) => ContainsAll(value, "bos", "daire") || ContainsAll(value, "satilmayan", "daire") || ContainsAny(value, "uygun daire", "musait daire");
    private static bool IsSoldApartmentCommand(string value) => ContainsAll(value, "satilan", "daire") || ContainsAll(value, "satilmis", "daire");
    private static bool IsExpenseQueryCommand(string value) => ContainsAll(value, "gider", "goster") || ContainsAll(value, "gider", "liste") || ContainsAny(value, "gider sorgula", "bu ayki gider", "aylik gider", "harcamalari goster", "masraflari goster");
    private static bool IsDashboardSummaryCommand(string value) => ContainsAny(value, "sistem ozeti", "genel ozet", "dashboard", "durum ozeti");
    private static bool IsProjectCreateCommand(string value) => ContainsAll(value, "proje", "ekle") || ContainsAll(value, "yeni", "proje");
    private static bool LooksLikeExpenseCreate(string value) => ContainsAny(value, "gider ekle", "geldi", "alindi", "eklendi") && ContainsAny(value, "tl", "tanesi", "birim");

    private async Task<Project?> FindProjectFromCommandAsync(string normalizedCommand)
    {
        var projects = await _context.Projects.AsNoTracking().OrderByDescending(p => p.Name.Length).ToListAsync();
        foreach (var project in projects)
        {
            var name = Normalize(project.Name);
            if (normalizedCommand.Contains(name))
            {
                return project;
            }
        }

        var tokens = normalizedCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 2 && !ProjectStopWords.Contains(t))
            .ToList();

        return projects
            .Select(p => new { Project = p, Score = tokens.Count(t => Normalize(p.Name).Contains(t)) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Project)
            .FirstOrDefault();
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

    private static bool ContainsAny(string value, params string[] needles) => needles.Any(value.Contains);
    private static bool ContainsAll(string value, params string[] needles) => needles.All(value.Contains);
    private static bool IsExactAny(string value, params string[] options) => options.Any(option => string.Equals(value, option, StringComparison.OrdinalIgnoreCase));

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
