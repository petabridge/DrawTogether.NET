using FluentEmail.Core.Interfaces;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DrawTogether.Email;

/// <summary>
/// Wraps FluentEmail to send emails using MailGun within the context of ASP.NET Core Identity.
/// </summary>
public sealed class MailGunEmailSender : IEmailSender
{
    private readonly ILogger<MailGunEmailSender> _logger;
    private readonly ISender _mailgunSender;
    private readonly EmailSettings _emailSettings;

    public MailGunEmailSender(ISender mailgunSender, IOptions<EmailSettings> emailSettings, ILogger<MailGunEmailSender> logger)
    {
        _logger = logger;
        _mailgunSender = mailgunSender;
        _emailSettings = emailSettings.Value;
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        _logger.LogDebug("Sending email to {Email} with subject {Subject}", email, subject);
        
        var mailMessage = FluentEmail.Core.Email
            .From(_emailSettings.FromAddress, _emailSettings.FromName)
            .To(email)
            .Subject(subject)
            .Body(htmlMessage, true);
        
        return _mailgunSender.SendAsync(mailMessage);
    }
}