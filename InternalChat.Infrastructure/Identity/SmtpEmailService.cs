using System.Net;
using System.Net.Mail;
using InternalChat.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InternalChat.Infrastructure.Identity;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string bodyHtml)
    {
        var smtpHost = _configuration["Smtp:Host"]!;
        var smtpPort = int.Parse(_configuration["Smtp:Port"]!);
        var smtpUser = _configuration["Smtp:Username"]!;
        var smtpPass = _configuration["Smtp:Password"]!;
        var fromName = _configuration["Smtp:FromName"] ?? "Internal Chat Support";

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            Credentials = new NetworkCredential(smtpUser, smtpPass),
            EnableSsl = true
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(smtpUser, fromName),
            Subject = subject,
            Body = bodyHtml,
            IsBodyHtml = true
        };
        mailMessage.To.Add(toEmail);

        try
        {
            _logger.LogInformation("Attempting to send reset password email to {ToEmail} via {Host}", toEmail, smtpHost);
            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
            throw new Exception("Could not send password reset email. Please try again later.", ex);
        }
    }
}
