using System.ComponentModel.DataAnnotations;

namespace OfisYonetimSistemi.Models;

public class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public string SmtpHost { get; set; } = "smtp.gmail.com";

    [Range(1, 65535)]
    public int SmtpPort { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    public string SenderName { get; set; } = "Smart Office";

    public string SenderEmail { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(SenderEmail)
        && !string.IsNullOrWhiteSpace(ResolvedUsername)
        && !string.IsNullOrWhiteSpace(Password);

    public string ResolvedUsername =>
        string.IsNullOrWhiteSpace(Username) ? SenderEmail : Username;
}
