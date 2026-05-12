using System.Globalization;
using System.Text.RegularExpressions;

namespace OfisYonetimSistemi.Localization;

public static class AppLanguage
{
    public const string CookieName = "smartoffice_lang";
    public const string Turkish = "tr";
    public const string English = "en";

    private static readonly Dictionary<string, string> EnglishTexts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ActiveSystem"] = "Active System",
        ["AdminLogin"] = "Admin Login",
        ["AdminLoginButton"] = "Sign in as Admin",
        ["AdminLoginHelp"] = "Authorized users sign in here to manage the system.",
        ["AdminPanel"] = "Admin Panel",
        ["AllRights"] = "All rights reserved by Smart Office.",
        ["Assistant"] = "Smart Office AI Assistant",
        ["BackToLoginChoice"] = "Back to sign-in options",
        ["BackToProjects"] = "Back to Projects",
        ["Cancel"] = "Cancel",
        ["CreateAccount"] = "Create Account",
        ["Dashboard"] = "Dashboard",
        ["Documents"] = "Documents",
        ["ExpenseAdd"] = "Add Expense",
        ["ExpenseAmount"] = "Total Price",
        ["ExpenseDate"] = "Entry Date",
        ["ExpenseDescription"] = "Description (optional)",
        ["ExpenseDocument"] = "Upload Receipt / Document",
        ["ExpenseDocumentOptional"] = "(optional)",
        ["ExpenseHeroText"] = "Save project expense, quantity and price information.",
        ["ExpenseItem"] = "Expense Item / Product",
        ["ExpenseEdit"] = "Edit Expense",
        ["ExpenseEditText"] = "Update the saved expense information.",
        ["ExpenseNew"] = "New Expense",
        ["ExpenseSave"] = "Save",
        ["ExpenseSupplier"] = "Company / Supplier",
        ["ExpenseUpdate"] = "Update",
        ["FileSaved"] = "Saved document:",
        ["HomeHeroEyebrow"] = "Smart Office Management System",
        ["HomeHeroTitle"] = "Manage projects, expenses and documents from one panel.",
        ["HomeHeroText"] = "Track projects, companies, invoices, documents and office workflows in a clean and readable management panel built for construction and office teams.",
        ["HomeIntroEyebrow"] = "Organized Management Experience",
        ["HomeIntroTitle"] = "A readable, controlled and centralized system instead of scattered files and verbal follow-ups.",
        ["HomeIntroText"] = "Track which documents, expenses, apartments and records belong to each project from a single screen.",
        ["HomeForWhom"] = "Who Is It For?",
        ["HomeForWhomText"] = "Designed for contractor offices, accounting teams and project managers.",
        ["HomeCtaTitle"] = "Make office management more organized and traceable.",
        ["HomeCtaText"] = "Sign in to your account or create a new user to start using the system.",
        ["Language"] = "Language",
        ["Login"] = "Sign In",
        ["LoginChoice"] = "Sign-in Options",
        ["LoginChoiceTitle"] = "Sign in to Smart Office with the right role.",
        ["LoginChoiceText"] = "Continue from secure sign-in screens separated by your permission level for projects, documents, expenses, personnel and apartment sales.",
        ["LoginManagerArea"] = "Management Area",
        ["LoginManagerTitle"] = "Admin / Manager Login",
        ["LoginManagerText"] = "Authorized panel for managing projects, users, expenses, documents and sales processes.",
        ["LoginPersonnelArea"] = "Personnel Area",
        ["LoginPersonnelTitle"] = "Personnel Login",
        ["LoginPersonnelText"] = "A simple personnel panel for viewing daily records and following project workflows.",
        ["Management"] = "Management",
        ["ManagerReports"] = "Dashboard and management reports",
        ["Panel"] = "Panel",
        ["Password"] = "Password",
        ["Personnel"] = "Personnel",
        ["PersonnelFocusedScreens"] = "Task-oriented screens",
        ["PersonnelLogin"] = "Personnel Login",
        ["PersonnelLoginButton"] = "Sign in as Personnel",
        ["PersonnelLoginHelp"] = "Office personnel sign in here for daily record and follow-up tasks.",
        ["PersonnelPanel"] = "Personnel Panel",
        ["ProjectPortfolio"] = "Project Portfolio",
        ["ProjectList"] = "Project List",
        ["ProjectManagement"] = "Project Management",
        ["ProjectManagementText"] = "Track project work with expenses, documents, apartments and team processes in one portfolio.",
        ["ProjectAdd"] = "Add Project",
        ["ProjectNew"] = "New Project",
        ["ProjectCount"] = "projects",
        ["ProjectEmptyTitle"] = "No projects have been added yet",
        ["ProjectEmptyText"] = "Create the first project to collect expenses, documents and apartment tracking in one place.",
        ["ProjectFirstAdd"] = "Add First Project",
        ["ProjectEnter"] = "Open Project",
        ["ProjectSmartOffice"] = "Smart Office Project",
        ["ProjectNoDescription"] = "No description has been added for this project yet.",
        ["Location"] = "Location",
        ["Start"] = "Start",
        ["End"] = "End",
        ["InProgress"] = "In progress",
        ["Edit"] = "Edit",
        ["Delete"] = "Delete",
        ["Projects"] = "Projects",
        ["ProjectsGo"] = "Open Projects",
        ["Quantity"] = "Quantity",
        ["Register"] = "Register",
        ["Settings"] = "Settings",
        ["SignOut"] = "Sign Out",
        ["SystemFeatures"] = "System features",
        ["UnitPrice"] = "Unit Price",
        ["UserEmail"] = "Email Address",
        ["ViewProjects"] = "Open Project List",
        ["ProjectDetail"] = "Project Detail",
        ["ProjectSummary"] = "Project Summary",
        ["ProjectImages"] = "Project Images",
        ["ProjectImagesView"] = "View Project Images",
        ["ImagesLoaded"] = "images uploaded",
        ["MoreImages"] = "more images",
        ["Status"] = "Status",
        ["TotalExpense"] = "Total expense",
        ["RemainingTime"] = "Remaining time",
        ["Expired"] = "Expired",
        ["Month"] = "month",
        ["Day"] = "day",
        ["ProjectName"] = "Project name",
        ["StartDate"] = "Start date",
        ["EndDate"] = "End date",
        ["CreatedDate"] = "Created date",
        ["ProjectExpenseTotal"] = "Project expense total",
        ["FloorApartmentCount"] = "Floor / apartment count",
        ["Floor"] = "Floor",
        ["Apartment"] = "apartment",
        ["ApartmentSalesTable"] = "Apartment sales tracking table",
        ["NoApartments"] = "There are no apartment records for this project.",
        ["SelectionType"] = "Selection type",
        ["ColumnSelect"] = "Select column",
        ["RowSelect"] = "Select row",
        ["RowColumnNo"] = "Row / column no",
        ["RoomType"] = "Room type",
        ["UpdateRoomType"] = "Update Room Type",
        ["Column"] = "Column",
        ["Sold"] = "SOLD",
        ["Empty"] = "EMPTY",
        ["ProjectExpensesDocuments"] = "Project expenses and documents",
        ["ItemTaken"] = "Item",
        ["Description"] = "Description",
        ["ReceiptDocument"] = "Receipt / Document",
        ["Date"] = "Date",
        ["Action"] = "Action",
        ["None"] = "None",
        ["Document"] = "Document",
        ["Gallery"] = "Gallery",
        ["Close"] = "Close",
        ["PreviousImage"] = "Previous image",
        ["NextImage"] = "Next image"
    };

    private static readonly Dictionary<string, string> TurkishTexts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ActiveSystem"] = "Aktif Sistem",
        ["AdminLogin"] = "Admin Girisi",
        ["AdminLoginButton"] = "Admin Girisi Yap",
        ["AdminLoginHelp"] = "Tam yetkili kullanicilar sistem yonetimi icin buradan giris yapar.",
        ["AdminPanel"] = "Admin Paneli",
        ["AllRights"] = "Tum haklari Smart Office'e aittir.",
        ["Assistant"] = "Smart Office AI Asistan",
        ["BackToLoginChoice"] = "Giris secimine don",
        ["BackToProjects"] = "Projelere Don",
        ["Cancel"] = "Iptal",
        ["CreateAccount"] = "Uye Ol",
        ["Dashboard"] = "Dashboard",
        ["Documents"] = "Evraklar",
        ["ExpenseAdd"] = "Gider Ekle",
        ["ExpenseAmount"] = "Toplam Fiyat",
        ["ExpenseDate"] = "Eklenme Tarihi",
        ["ExpenseDescription"] = "Aciklama (istege bagli)",
        ["ExpenseDocument"] = "Fis / Belge Yukle",
        ["ExpenseDocumentOptional"] = "(istege bagli)",
        ["ExpenseHeroText"] = "Projeye ait gider, miktar ve fiyat bilgisini kaydedin.",
        ["ExpenseItem"] = "Gider Kalemi / Urun",
        ["ExpenseEdit"] = "Gider Duzenle",
        ["ExpenseEditText"] = "Kayitli gider bilgisini guncelleyin.",
        ["ExpenseNew"] = "Yeni Gider",
        ["ExpenseSave"] = "Kaydet",
        ["ExpenseSupplier"] = "Firma / Tedarikci",
        ["ExpenseUpdate"] = "Guncelle",
        ["FileSaved"] = "Kayitli belge:",
        ["HomeHeroEyebrow"] = "Akilli Ofis Yonetim Sistemi",
        ["HomeHeroTitle"] = "Proje, gider ve evrak sureclerini tek panelden yonetin.",
        ["HomeHeroText"] = "Insaat ve ofis ekipleri icin gelistirilen sade yonetim paneliyle proje, firma, fatura, evrak ve ofis sureclerini duzenli ve anlasilir bir yapida takip edin.",
        ["HomeIntroEyebrow"] = "Duzenli Yonetim Deneyimi",
        ["HomeIntroTitle"] = "Daginik dosyalar ve sozlu takip yerine okunabilir, kontrollu ve tek merkezli bir sistem.",
        ["HomeIntroText"] = "Hangi evrak, gider, daire ve kaydin hangi projeye ait oldugunu tek ekrandan takip edin.",
        ["HomeForWhom"] = "Kimler Icin?",
        ["HomeForWhomText"] = "Muteahhit ofisleri, muhasebe ekipleri ve proje sorumlulari icin tasarlandi.",
        ["HomeCtaTitle"] = "Ofis yonetimini daha duzenli ve izlenebilir hale getirin.",
        ["HomeCtaText"] = "Hesabiniza giris yapin veya yeni kullanici olusturarak sistemi kullanmaya baslayin.",
        ["Language"] = "Dil",
        ["Login"] = "Giris Yap",
        ["LoginChoice"] = "Giris Secimi",
        ["LoginChoiceTitle"] = "Smart Office paneline rolunuze uygun giris yapin.",
        ["LoginChoiceText"] = "Proje, evrak, gider, personel ve daire satis surecleri icin yetkinize gore ayrilmis guvenli giris ekranlarindan devam edin.",
        ["LoginManagerArea"] = "Yonetici Alani",
        ["LoginManagerTitle"] = "Admin / Mudur Girisi",
        ["LoginManagerText"] = "Projeleri, kullanicilari, giderleri, evrak kayitlarini ve satis sureclerini yonetmek icin yetkili panel.",
        ["LoginPersonnelArea"] = "Personel Alani",
        ["LoginPersonnelTitle"] = "Personel Girisi",
        ["LoginPersonnelText"] = "Gunluk kayitlari goruntulemek, evrak ve proje sureclerini takip etmek icin sade personel paneli.",
        ["Management"] = "Yonetim",
        ["ManagerReports"] = "Dashboard ve yonetim raporlari",
        ["Panel"] = "Panel",
        ["Password"] = "Sifre",
        ["Personnel"] = "Personeller",
        ["PersonnelFocusedScreens"] = "Gorev odakli ekranlar",
        ["PersonnelLogin"] = "Personel Girisi",
        ["PersonnelLoginButton"] = "Personel Girisi Yap",
        ["PersonnelLoginHelp"] = "Ofis personeli gunluk kayit ve takip islemleri icin buradan giris yapar.",
        ["PersonnelPanel"] = "Personel Paneli",
        ["ProjectPortfolio"] = "Proje Portfoyu",
        ["ProjectList"] = "Proje Listesi",
        ["ProjectManagement"] = "Proje Yonetimi",
        ["ProjectManagementText"] = "Ofiste takip edilen isleri proje bazli kaydedin; gider, evrak, daire ve ekip sureclerini tek portfoy uzerinden izleyin.",
        ["ProjectAdd"] = "Proje Ekle",
        ["ProjectNew"] = "Yeni Proje",
        ["ProjectCount"] = "proje",
        ["ProjectEmptyTitle"] = "Henuz proje eklenmedi",
        ["ProjectEmptyText"] = "Ilk projeyi olusturarak gider, evrak ve daire takiplerini proje icinde toplamaya baslayin.",
        ["ProjectFirstAdd"] = "Ilk Projeyi Ekle",
        ["ProjectEnter"] = "Projeye Gir",
        ["ProjectSmartOffice"] = "Smart Office Proje",
        ["ProjectNoDescription"] = "Bu proje icin henuz aciklama eklenmedi.",
        ["Location"] = "Konum",
        ["Start"] = "Baslangic",
        ["End"] = "Bitis",
        ["InProgress"] = "Devam ediyor",
        ["Edit"] = "Duzenle",
        ["Delete"] = "Sil",
        ["Projects"] = "Projeler",
        ["ProjectsGo"] = "Projeleri Ac",
        ["Quantity"] = "Miktar",
        ["Register"] = "Uye Ol",
        ["Settings"] = "Ayarlar",
        ["SignOut"] = "Sistemden Cikis Yap",
        ["SystemFeatures"] = "Sistem ozellikleri",
        ["UnitPrice"] = "Birim Fiyat",
        ["UserEmail"] = "E-posta Adresi",
        ["ViewProjects"] = "Proje Listesine Git",
        ["ProjectDetail"] = "Proje Detay",
        ["ProjectSummary"] = "Proje Ozeti",
        ["ProjectImages"] = "Proje Gorselleri",
        ["ProjectImagesView"] = "Proje Gorsellerini Gor",
        ["ImagesLoaded"] = "gorsel yuklendi",
        ["MoreImages"] = "gorsel daha",
        ["Status"] = "Durum",
        ["TotalExpense"] = "Toplam gider",
        ["RemainingTime"] = "Kalan sure",
        ["Expired"] = "Suresi doldu",
        ["Month"] = "ay",
        ["Day"] = "gun",
        ["ProjectName"] = "Proje adi",
        ["StartDate"] = "Baslangic tarihi",
        ["EndDate"] = "Bitis tarihi",
        ["CreatedDate"] = "Kayit tarihi",
        ["ProjectExpenseTotal"] = "Proje gider toplami",
        ["FloorApartmentCount"] = "Kat / daire sayisi",
        ["Floor"] = "Kat",
        ["Apartment"] = "daire",
        ["ApartmentSalesTable"] = "Daire satis takip tablosu",
        ["NoApartments"] = "Bu proje icin daire kaydi bulunmuyor.",
        ["SelectionType"] = "Secim tipi",
        ["ColumnSelect"] = "Sutun sec",
        ["RowSelect"] = "Satir sec",
        ["RowColumnNo"] = "Satir / Sutun no",
        ["RoomType"] = "Oda tipi",
        ["UpdateRoomType"] = "Oda Tipini Guncelle",
        ["Column"] = "Sutun",
        ["Sold"] = "SATILDI",
        ["Empty"] = "BOS",
        ["ProjectExpensesDocuments"] = "Projeye ait gider ve belgeler",
        ["ItemTaken"] = "Alinan Malzeme",
        ["Description"] = "Aciklama",
        ["ReceiptDocument"] = "Fis / Belge",
        ["Date"] = "Tarih",
        ["Action"] = "Islem",
        ["None"] = "Yok",
        ["Document"] = "Belge",
        ["Gallery"] = "Galeri",
        ["Close"] = "Kapat",
        ["PreviousImage"] = "Onceki gorsel",
        ["NextImage"] = "Sonraki gorsel"
    };

    private static readonly Dictionary<string, string> EnglishLogText = new(StringComparer.Ordinal)
    {
        ["Sistem Takibi"] = "System Tracking",
        ["Son Islemler"] = "Recent Actions",
        ["Son İşlemler"] = "Recent Actions",
        ["Son Ä°ÅŸlemler"] = "Recent Actions",
        ["Tumunu Gor"] = "View All",
        ["Tümünü Gör"] = "View All",
        ["TÃ¼mÃ¼nÃ¼ GÃ¶r"] = "View All",
        ["Basarili"] = "Successful",
        ["Başarılı"] = "Successful",
        ["BaÅŸarÄ±lÄ±"] = "Successful",
        ["Basarisiz"] = "Failed",
        ["Başarısız"] = "Failed",
        ["BaÅŸarÄ±sÄ±z"] = "Failed",
        ["Ekleme"] = "Add",
        ["Giris"] = "Login",
        ["Giriş"] = "Login",
        ["Cikis"] = "Sign Out",
        ["Çıkış"] = "Sign Out",
        ["Oturum"] = "Session",
        ["Giderler"] = "Expenses",
        ["Projeler"] = "Projects",
        ["Personeller"] = "Personnel",
        ["Admin/Manager girisi yapildi."] = "Admin/Manager signed in.",
        ["Admin/Manager girişi yapıldı."] = "Admin/Manager signed in.",
        ["Admin/Manager giriÅŸi yapÄ±ldÄ±."] = "Admin/Manager signed in.",
        ["Admin/Manager logini yapildi."] = "Admin/Manager signed in.",
        ["Admin/Manager logini yapıldı."] = "Admin/Manager signed in.",
        ["Admin/Manager login yapildi."] = "Admin/Manager signed in.",
        ["Admin/Manager login yapıldı."] = "Admin/Manager signed in.",
        ["logini yapildi."] = "signed in.",
        ["logini yapıldı."] = "signed in.",
        ["login yapildi."] = "signed in.",
        ["login yapıldı."] = "signed in.",
        ["yapildi."] = "completed.",
        ["yapıldı."] = "completed.",
        ["gideri eklendi. Amount:"] = "expense added. Amount:",
        ["gideri eklendi. Tutar:"] = "expense added. Amount:",
        ["personel hesabi olusturuldu."] = "personnel account was created.",
        ["projesi olusturuldu."] = "project was created.",
        ["Sistem Admin"] = "System Admin",
        ["Bilinmeyen Kullanici"] = "Unknown User",
        ["Bilinmeyen Rol"] = "Unknown Role"
    };

    public static string Current(HttpContext context)
    {
        var cookieValue = context.Request.Cookies[CookieName];
        return string.Equals(cookieValue, English, StringComparison.OrdinalIgnoreCase) ? English : Turkish;
    }

    public static bool IsEnglish(HttpContext context)
    {
        return Current(context) == English;
    }

    public static CultureInfo Culture(HttpContext context)
    {
        return IsEnglish(context) ? new CultureInfo("en-US") : new CultureInfo("tr-TR");
    }

    public static string T(HttpContext context, string key)
    {
        var texts = IsEnglish(context) ? EnglishTexts : TurkishTexts;
        return texts.TryGetValue(key, out var value) ? value : key;
    }

    public static string LogText(HttpContext context, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        if (!IsEnglish(context))
        {
            return text;
        }

        var result = text;
        foreach (var pair in EnglishLogText.OrderByDescending(item => item.Key.Length))
        {
            result = result.Replace(pair.Key, pair.Value, StringComparison.Ordinal);
        }

        result = NormalizeEnglishLogGrammar(result);
        return NormalizeEnglishCurrency(result);
    }

    private static string NormalizeEnglishCurrency(string text)
    {
        return Regex.Replace(
            text,
            @"\b(\d{1,3}(?:\.\d{3})+),(\d{2})\s*TL\b",
            match => $"{match.Groups[1].Value.Replace(".", ",")}.{match.Groups[2].Value} TL");
    }

    private static string NormalizeEnglishLogGrammar(string text)
    {
        var result = text;
        result = Regex.Replace(result, @"\bAdmin/Manager\s+login[ıi]?\s+completed\.", "Admin/Manager signed in.", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\bAdmin/Manager\s+login[ıi]?\s+yap[ıi]ld[ıi]\.", "Admin/Manager signed in.", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\blogin[ıi]?\s+completed\.", "signed in.", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\blogin[ıi]?\s+yap[ıi]ld[ıi]\.", "signed in.", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\s{2,}", " ");
        return result.Trim();
    }
}
