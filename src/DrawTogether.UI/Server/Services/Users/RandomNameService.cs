using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace DrawTogether.UI.Server.Services.Users;

public sealed class RandomNameService : IUserIdProvider
{
    // DO NOT DO THIS IN PRODUCTION
    private readonly ConcurrentDictionary<string, string> _connectionIdToNames = new();

    public string? GetUserId(HubConnectionContext connection)
    {
        if (_connectionIdToNames.ContainsKey(connection.ConnectionId))
        {
            return _connectionIdToNames[connection.ConnectionId];
        }

        var name = _connectionIdToNames[connection.ConnectionId] = UserNamingService.GenerateRandomName();
        return name;
    }
}