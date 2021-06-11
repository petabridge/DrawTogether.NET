// -----------------------------------------------------------------------
// <copyright file="IDrawHubHandler.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2021 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
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
        Task AddPointsToConnectedStroke(string sessionId, Guid Id, Point[] points);

        Task AddPointsToConnectedStroke(string connectionId, string sessionId, Guid Id, Point[] points);

        Task CreateNewConnectedStroke(string sessionId, ConnectedStroke connectedStroke);

        Task CreateNewConnectedStroke(string connectionId, string sessionId, ConnectedStroke connectedStroke);

        Task PushConnectedStrokes(string sessionId, ConnectedStroke[] connectedStrokes);

        Task PushConnectedStrokes(string connectionId, string sessionId, ConnectedStroke[] connectedStrokes);

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

        public async Task AddPointsToConnectedStroke(string sessionId, Guid Id, Point[] points)
        {
            await _drawHub.Clients.Group(sessionId).SendAsync("AddPointsToConnectedStroke", Id, points).ConfigureAwait(false);
        }

        public async Task AddPointsToConnectedStroke(string connectionId, string sessionId, Guid Id, Point[] points)
        {
            await _drawHub.Clients.Client(connectionId).SendAsync("AddPointsToConnectedStroke", Id, points).ConfigureAwait(false);
        }

        public async Task CreateNewConnectedStroke(string sessionId, ConnectedStroke connectedStroke)
        {
            await _drawHub.Clients.Group(sessionId).SendAsync("CreateConnectedStroke", connectedStroke).ConfigureAwait(false);
        }

        public async Task CreateNewConnectedStroke(string connectionId, string sessionId, ConnectedStroke connectedStroke)
        {
            await _drawHub.Clients.Client(connectionId).SendAsync("CreateConnectedStroke", connectedStroke).ConfigureAwait(false);
        }

        public async Task PushConnectedStrokes(string sessionId, ConnectedStroke[] connectedStrokes)
        {
            await _drawHub.Clients.Group(sessionId).SendAsync("AddConnectedStrokes", connectedStrokes).ConfigureAwait(false);
        }

        public async Task PushConnectedStrokes(string connectionId, string sessionId, ConnectedStroke[] connectedStrokes)
        {
            await _drawHub.Clients.Client(connectionId).SendAsync("AddConnectedStrokes", connectedStrokes).ConfigureAwait(false);
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