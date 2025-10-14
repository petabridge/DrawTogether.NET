using DrawTogether.Entities.Users;
using Microsoft.JSInterop;

namespace DrawTogether.Services;

/// <summary>
/// Service that manages anonymous user identities using browser localStorage
/// </summary>
public class AnonymousUserService : IAnonymousUserService
{
    private readonly IJSRuntime _jsRuntime;
    private UserId? _cachedUserId;

    public AnonymousUserService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <inheritdoc />
    public async Task<UserId> GetOrCreateAnonymousUserIdAsync()
    {
        // Return cached value if we already have it
        if (_cachedUserId is not null)
        {
            return _cachedUserId;
        }

        // Call JavaScript to get or create the anonymous user ID
        var anonymousId = await _jsRuntime.InvokeAsync<string>(
            "anonymousUserStorage.getOrCreateUserId");

        _cachedUserId = new UserId(anonymousId);
        return _cachedUserId;
    }
}
