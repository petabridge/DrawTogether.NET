using DrawTogether.Config;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace DrawTogether.Authorization;

/// <summary>
/// Handles the DrawingAccessRequirement by checking if anonymous access is enabled
/// or if the user is authenticated.
/// </summary>
public class DrawingAccessHandler : AuthorizationHandler<DrawingAccessRequirement>
{
    private readonly IOptions<DrawTogetherSettings> _settings;

    public DrawingAccessHandler(IOptions<DrawTogetherSettings> settings)
    {
        _settings = settings;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DrawingAccessRequirement requirement)
    {
        // If anonymous access is enabled, always succeed
        if (_settings.Value.AllowAnonymousAccess)
        {
            context.Succeed(requirement);
        }
        // If anonymous access is disabled, require authentication
        else if (context.User.Identity?.IsAuthenticated == true)
        {
            context.Succeed(requirement);
        }
        // Otherwise fail (will redirect to login via RedirectToLogin component)

        return Task.CompletedTask;
    }
}
