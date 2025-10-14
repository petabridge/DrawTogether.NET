using Microsoft.Extensions.Options;

namespace DrawTogether.Config;

public class DrawTogetherSettings
{
    /// <summary>
    /// We disable this using the default repo settings so users can run the app locally right away
    /// </summary>
    public bool RequireEmailValidation { get; set; }

    /// <summary>
    /// When true, users can access drawing sessions without creating an account.
    /// Anonymous users are assigned temporary IDs stored in browser localStorage.
    /// Default is true to allow immediate access.
    /// </summary>
    public bool AllowAnonymousAccess { get; set; } = true;
}

public sealed class DrawTogetherSettingsValidator : IValidateOptions<DrawTogetherSettings>
{
    public ValidateOptionsResult Validate(string? name, DrawTogetherSettings options)
    {
        var errors = new List<string>();
        
        return errors.Count > 0 ? ValidateOptionsResult.Fail(errors) : ValidateOptionsResult.Success;
    }
}

public static class DrawTogetherSettingsExtensions
{
    public static IServiceCollection AddDrawTogetherSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DrawTogetherSettings>()
            .ValidateOnStart().BindConfiguration("DrawTogether");
        
        return services;
    }
}