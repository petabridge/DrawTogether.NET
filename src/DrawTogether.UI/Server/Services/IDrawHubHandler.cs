// -----------------------------------------------------------------------
// <copyright file="IDrawHubHandler.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2021 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using DrawTogether.UI.Server.Hubs;
using DrawTogether.UI.Shared;
using Microsoft.AspNetCore.SignalR;

namespace DrawTogether.UI.Server.Services
{
    /// <summary>
    /// Used for Akka.NET --> SignalR interop
    /// </summary>
    public interface IDrawHubHandler
    {
        Task PushStrokes(string sessionId, StrokeData[] strokes);

        Task PushStrokes(string connectionId, string sessionId, StrokeData[] strokes);

        Task AddUser(string sessionId, string userName);

        Task AddUsers(string connectionId, IEnumerable<string> userNames);
    }

    internal sealed class DrawHubHandler : IDrawHubHandler
    {
        private readonly IHubContext<DrawHub> _drawHub;

        public DrawHubHandler(IHubContext<DrawHub> drawHub)
        {
            _drawHub = drawHub;
        }

        public async Task PushStrokes(string sessionId, StrokeData[] strokes)
        {
            await _drawHub.Clients.Group(sessionId).SendAsync("DrawStrokes", strokes).ConfigureAwait(false);
        }

        public async Task PushStrokes(string connectionId, string sessionId, StrokeData[] strokes)
        {
            await _drawHub.Clients.Client(connectionId).SendAsync("DrawStrokes", strokes).ConfigureAwait(false);
        }

        public async Task AddUser(string sessionId, string userName)
        {
            await _drawHub.Clients.Group(sessionId).SendAsync("AddUser", userName).ConfigureAwait(false);
        }

        public async Task AddUsers(string connectionId, IEnumerable<string> userNames)
        {
            foreach (var u in userNames)
            {
                await _drawHub.Clients.Client(connectionId).SendAsync("AddUser", u).ConfigureAwait(false);
            }
        }
    }
}