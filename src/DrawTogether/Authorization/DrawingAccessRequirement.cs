using Microsoft.AspNetCore.Authorization;

namespace DrawTogether.Authorization;

/// <summary>
/// Authorization requirement for accessing drawing sessions.
/// This requirement is satisfied when:
/// - Anonymous access is enabled (regardless of authentication status), OR
/// - Anonymous access is disabled AND user is authenticated
/// </summary>
public class DrawingAccessRequirement : IAuthorizationRequirement
{
    // Marker class - no additional properties needed
}
