using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace DrawTogether.UI.Server.Services.Users
{
    public sealed class RandomNameService : IUserIdProvider
    {
        // DO NOT DO THIS IN PRODUCTION
        private readonly ConcurrentDictionary<string, string> _connectionIdToNames =
            new ConcurrentDictionary<string, string>();

        public string? GetUserId(HubConnectionContext connection)
        {
            if (_connectionIdToNames.ContainsKey(connection.ConnectionId))
                return _connectionIdToNames[connection.ConnectionId];
            else
            {
                var name = _connectionIdToNames[connection.ConnectionId] = UserNamingService.GenerateRandomName();
                return name;
            }
        }
    }
}
