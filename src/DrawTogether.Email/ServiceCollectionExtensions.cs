using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DrawTogether.Email;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEmailServices<TUser>(this IServiceCollection services, IConfiguration configuration) where TUser : class
    {
        // check to see if the EmailSettings section is configured in the appsettings.json file
        if (!configuration.GetSection("EmailSettings").Exists())
        {
            // bail out early if email is not configured
            return services;
        }
        
        // also check to see if either of the Mailgun settings are configured
        if (string.IsNullOrWhiteSpace(configuration["EmailSettings:MailgunDomain"]) ||
            string.IsNullOrWhiteSpace(configuration["EmailSettings:MailgunApiKey"]))
        {
            return services;
        }
        
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

        services.AddFluentEmail(configuration["EmailSettings:FromAddress"], configuration["EmailSettings:FromName"])
            .AddRazorRenderer()
            .AddMailGunSender(configuration["EmailSettings:MailgunDomain"],
                configuration["EmailSettings:MailgunApiKey"]);

        services.AddTransient<IEmailSender, MailGunEmailSender<TUser>>();
        services.AddTransient<IEmailSender<TUser>, MailGunEmailSender<TUser>>();

        return services;
    }
}