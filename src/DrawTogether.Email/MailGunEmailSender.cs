using FluentEmail.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DrawTogether.Email;

/// <summary>
/// Wraps FluentEmail to send emails using MailGun within the context of ASP.NET Core Identity.
/// </summary>
public sealed class MailGunEmailSender<TUser> : IEmailSender<TUser>, IEmailSender where TUser : class
{
    private readonly ILogger<MailGunEmailSender<TUser>> _logger;
    private readonly ISender _mailgunSender;
    private readonly EmailSettings _emailSettings;

    public MailGunEmailSender(ISender mailgunSender, IOptions<EmailSettings> emailSettings, ILogger<MailGunEmailSender<TUser>> logger)
    {
        _logger = logger;
        _mailgunSender = mailgunSender;
        _emailSettings = emailSettings.Value;
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        _logger.LogInformation("Sending email to {Email} with subject {Subject}", email, subject);
        
        var mailMessage = FluentEmail.Core.Email
            .From(_emailSettings.FromAddress, _emailSettings.FromName)
            .To(email)
            .Subject(subject)
            .Body(htmlMessage, true);
        
        return _mailgunSender.SendAsync(mailMessage);
    }

    public Task SendConfirmationLinkAsync(TUser user, string email, string confirmationLink)
    {
        // format an email with the confirmation link
        var subject = "Confirm your email with DrawTogether";
        var body = $"Please confirm your email by clicking this link: <a href='{confirmationLink}'>Confirm Email</a>";
        
        return SendEmailAsync(email, subject, body);
    }

    public Task SendPasswordResetLinkAsync(TUser user, string email, string resetLink)
    {
        // format an email with the reset link
        var subject = "Reset your password with DrawTogether";
        var body = $"Please reset your password by clicking this link: <a href='{resetLink}'>Reset Password</a>";
        
        return SendEmailAsync(email, subject, body);
    }

    public Task SendPasswordResetCodeAsync(TUser user, string email, string resetCode)
    {
        // format an email with the reset code
        var subject = "Reset your password with DrawTogether";
        var body = $"Please reset your password using this code: {resetCode}";
        
        return SendEmailAsync(email, subject, body);
    }
}