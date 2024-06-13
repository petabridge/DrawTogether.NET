using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace DrawTogether.Email;

/// <summary>
/// Marker interface to signal that email is not enabled.
/// </summary>
public interface INoOpEmailSender : IEmailSender { }

/// <summary>
/// Had to implement this because the ASP.NET Core version does not support <see cref="IEmailSender{TUser}"/>
/// </summary>
public sealed class NoOpEmailSender<TUser> : IEmailSender<TUser>, INoOpEmailSender where TUser : class
{
    public Task SendConfirmationLinkAsync(TUser user, string email, string confirmationLink)
    {
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(TUser user, string email, string resetLink)
    {
        return Task.CompletedTask;
    }

    public Task SendPasswordResetCodeAsync(TUser user, string email, string resetCode)
    {
        return Task.CompletedTask;
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        return Task.CompletedTask;
    }
}