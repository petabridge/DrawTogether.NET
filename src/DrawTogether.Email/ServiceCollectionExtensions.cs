using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DrawTogether.Email;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEmailServices(this IServiceCollection services, IConfiguration configuration)
    {
        // check to see if the EmaiLSettings section is configured in the appsettings.json file
        if (!configuration.GetSection("EmailSettings").Exists())
        {
            // bail out early if email is not configured
            return services;
        }
        
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

        services.AddFluentEmail(configuration["EmailSettings:FromAddress"], configuration["EmailSettings:FromName"])
            .AddRazorRenderer()
            .AddMailGunSender(configuration["EmailSettings:MailgunDomain"],
                configuration["EmailSettings:MailgunApiKey"]);

        services.AddTransient<IEmailSender, MailGunEmailSender>();

        return services;
    }
}