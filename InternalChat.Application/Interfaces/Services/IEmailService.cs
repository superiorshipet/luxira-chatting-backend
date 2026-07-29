namespace InternalChat.Application.Interfaces.Services;

/// <summary>
/// Service to handle sending emails.
/// </summary>
public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string bodyHtml);
}
