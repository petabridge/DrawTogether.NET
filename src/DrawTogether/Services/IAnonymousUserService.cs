using DrawTogether.Entities.Users;

namespace DrawTogether.Services;

/// <summary>
/// Service for managing anonymous user identities stored in browser localStorage
/// </summary>
public interface IAnonymousUserService
{
    /// <summary>
    /// Gets or creates an anonymous user ID for the current browser session.
    /// The ID is persisted in localStorage to maintain identity across page refreshes.
    /// </summary>
    /// <returns>A UserId for the anonymous user</returns>
    Task<UserId> GetOrCreateAnonymousUserIdAsync();
}
