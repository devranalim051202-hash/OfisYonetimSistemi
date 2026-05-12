using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;
using OfisYonetimSistemi.Models;

namespace OfisYonetimSistemi.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailSettings> settings, ILogger<SmtpEmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        if (!_settings.IsConfigured)
        {
            throw new InvalidOperationException("Gmail ayarları eksik. appsettings.Development.json içindeki SenderEmail, Username ve Password alanlarını doldurun.");
        }

        var password = _settings.Password.Replace(" ", string.Empty);

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            EnableSsl = _settings.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_settings.ResolvedUsername, password)
        };

        try
        {
            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Verification email could not be sent to {Email}.", toEmail);
            throw;
        }
    }
}
