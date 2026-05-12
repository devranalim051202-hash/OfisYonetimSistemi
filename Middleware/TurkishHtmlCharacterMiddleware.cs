using System.Text;
using System.Text.RegularExpressions;
using OfisYonetimSistemi.Localization;

namespace OfisYonetimSistemi.Middleware;

public class TurkishHtmlCharacterMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly Dictionary<string, string> PhraseFixes = new(StringComparer.Ordinal)
    {
        ["Akilli Ofis"] = "Akıllı Ofis",
        ["Ofis Yonetimi"] = "Ofis Yönetimi",
        ["Ofis Yonetim Sistemi"] = "Ofis Yönetim Sistemi",
        ["Akilli Ofis Yonetim Sistemi"] = "Akıllı Ofis Yönetim Sistemi",
        ["Yonetim Paneli"] = "Yönetim Paneli",
        ["Proje Yonetimi"] = "Proje Yönetimi",
        ["Proje Bazli Yonetim"] = "Proje Bazlı Yönetim",
        ["Duzenli Yonetim Deneyimi"] = "Düzenli Yönetim Deneyimi",
        ["Kurumsal Altyapi"] = "Kurumsal Altyapı",
        ["Giris Yap"] = "Giriş Yap",
        ["Giris Secimi"] = "Giriş Seçimi",
        ["Giris secimine don"] = "Giriş seçimine dön",
        ["Admin Girisi"] = "Admin Girişi",
        ["Admin Girisi Yap"] = "Admin Girişi Yap",
        ["Personel Girisi"] = "Personel Girişi",
        ["Personel Girisi Yap"] = "Personel Girişi Yap",
        ["Uye Ol"] = "Üye Ol",
        ["Uye ol"] = "Üye ol",
        ["Sifre"] = "Şifre",
        ["Cikis Yap"] = "Çıkış Yap",
        ["Sistemden Cikis Yap"] = "Sistemden Çıkış Yap",
        ["Tum haklari"] = "Tüm hakları",
        ["Tumunu Gor"] = "Tümünü Gör",
        ["Son Islemler"] = "Son İşlemler",
        ["Islem Loglari"] = "İşlem Logları",
        ["Proje Portfoyu"] = "Proje Portföyü",
        ["Proje Ozeti"] = "Proje Özeti",
        ["Proje Gorselleri"] = "Proje Görselleri",
        ["Proje Gorsellerini Gor"] = "Proje Görsellerini Gör",
        ["gorsel yuklendi"] = "görsel yüklendi",
        ["gorsel daha"] = "görsel daha",
        ["Daire Satis"] = "Daire Satış",
        ["Daire satis"] = "Daire satış",
        ["Satis Yap"] = "Satış Yap",
        ["Satis durumu"] = "Satış durumu",
        ["Satis bilgileri"] = "Satış bilgileri",
        ["Satis sozlesmesi"] = "Satış sözleşmesi",
        ["Satis bedeli"] = "Satış bedeli",
        ["Satis tarihi"] = "Satış tarihi",
        ["Satisi Tamamla"] = "Satışı Tamamla",
        ["Daireyi Guncelle"] = "Daireyi Güncelle",
        ["Daire Bilgilerini Duzenle"] = "Daire Bilgilerini Düzenle",
        ["Daire bilgileri"] = "Daire bilgileri",
        ["Daireye don"] = "Daireye dön",
        ["Projeye Don"] = "Projeye Dön",
        ["Projelere Don"] = "Projelere Dön",
        ["Proje adi"] = "Proje adı",
        ["Proje Adi"] = "Proje Adı",
        ["Proje kaydi"] = "Proje kaydı",
        ["Proje Detay"] = "Proje Detay",
        ["Proje Takibi"] = "Proje Takibi",
        ["Proje Sorumlulari"] = "Proje Sorumluları",
        ["Gider Yonetimi"] = "Gider Yönetimi",
        ["Gider Duzenle"] = "Gider Düzenle",
        ["Fis / Belge"] = "Fiş / Belge",
        ["Fis / Belge Yukle"] = "Fiş / Belge Yükle",
        ["Bağlı Proje"] = "Bağlı Proje",
        ["Aciklama"] = "Açıklama",
        ["Iptal"] = "İptal",
        ["Islem"] = "İşlem",
        ["Islemler"] = "İşlemler",
        ["Isleme nereden baslanir"] = "İşleme nereden başlanır",
        ["Kayit"] = "Kayıt",
        ["Kayitli"] = "Kayıtlı",
        ["kayit"] = "kayıt",
        ["Guncelle"] = "Güncelle",
        ["guncelleyin"] = "güncelleyin",
        ["Goruntule"] = "Görüntüle",
        ["goruntule"] = "görüntüle",
        ["goruntuleme"] = "görüntüleme",
        ["Yonetici"] = "Yönetici",
        ["Mudur"] = "Müdür",
        ["Muteahhitler"] = "Müteahhitler",
        ["Alinan Malzeme"] = "Alınan Malzeme",
        ["Kalan sure"] = "Kalan süre",
        ["Suresi doldu"] = "Süresi doldu",
        ["Baslangic"] = "Başlangıç",
        ["Bitis"] = "Bitiş",
        ["Satir"] = "Satır",
        ["Sutun"] = "Sütun",
        ["Secim"] = "Seçim",
        ["BOS"] = "BOŞ",
        ["SATILDI"] = "SATILDI"
    };

    private static readonly (string Pattern, string Replacement)[] WordFixes =
    {
        (@"\byonet", "yönet"),
        (@"\bYonet", "Yönet"),
        (@"\bsurec", "süreç"),
        (@"\bSurec", "Süreç"),
        (@"\bduzen", "düzen"),
        (@"\bDuzen", "Düzen"),
        (@"\bgiris", "giriş"),
        (@"\bGiris", "Giriş"),
        (@"\bcikis", "çıkış"),
        (@"\bCikis", "Çıkış"),
        (@"\bolustur", "oluştur"),
        (@"\bOlustur", "Oluştur"),
        (@"\bkullanici", "kullanıcı"),
        (@"\bKullanici", "Kullanıcı"),
        (@"\bguven", "güven"),
        (@"\bGuven", "Güven"),
        (@"\bgorsel", "görsel"),
        (@"\bGorsel", "Görsel"),
        (@"\bsozlesme", "sözleşme"),
        (@"\bSozlesme", "Sözleşme"),
        (@"\bsatis", "satış"),
        (@"\bSatis", "Satış"),
        (@"\bduzenle", "düzenle"),
        (@"\bDuzenle", "Düzenle"),
        (@"\bgorev", "görev"),
        (@"\bGorev", "Görev"),
        (@"\bicinde", "içinde"),
        (@"\bIcind", "İçind"),
        (@"\bicin", "için"),
        (@"\bIcin", "İçin"),
        (@"\bdegil", "değil"),
        (@"\bDegil", "Değil")
    };

    public TurkishHtmlCharacterMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (AppLanguage.IsEnglish(context) || !AcceptsHtml(context))
        {
            await _next(context);
            return;
        }

        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        await _next(context);

        if (!IsHtmlResponse(context))
        {
            buffer.Position = 0;
            await buffer.CopyToAsync(originalBody);
            context.Response.Body = originalBody;
            return;
        }

        buffer.Position = 0;
        using var reader = new StreamReader(buffer, Encoding.UTF8);
        var html = await reader.ReadToEndAsync();
        var fixedHtml = FixTurkishCharacters(html);
        var bytes = Encoding.UTF8.GetBytes(fixedHtml);

        context.Response.ContentLength = bytes.Length;
        context.Response.Body = originalBody;
        await context.Response.Body.WriteAsync(bytes);
    }

    private static bool AcceptsHtml(HttpContext context)
    {
        return context.Request.Headers.Accept.Any(value => value?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true)
            || string.IsNullOrEmpty(context.Request.Headers.Accept);
    }

    private static bool IsHtmlResponse(HttpContext context)
    {
        return context.Response.ContentType?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string FixTurkishCharacters(string html)
    {
        var result = html;

        foreach (var pair in PhraseFixes.OrderByDescending(pair => pair.Key.Length))
        {
            result = result.Replace(pair.Key, pair.Value, StringComparison.Ordinal);
        }

        foreach (var (pattern, replacement) in WordFixes)
        {
            result = Regex.Replace(result, pattern, replacement, RegexOptions.CultureInvariant);
        }

        return result;
    }
}
