using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DrawTogether.UI.Server.Services;
using DrawTogether.UI.Server.Services.Users;
using DrawTogether.UI.Shared;
using DrawTogether.UI.Shared.Connectivity;
using Microsoft.AspNetCore.SignalR;

namespace DrawTogether.UI.Server.Hubs
{
    public sealed class DrawHub : Hub
    {
        private readonly IDrawSessionHandler _sessionHandler;

        public DrawHub(IDrawSessionHandler sessionHandler)
        {
            _sessionHandler = sessionHandler;
        }

        /// <summary>
        /// Connects this SignalR User to a paint session in progress.
        /// </summary>
        /// <param name="sessionId">The unique id of a specific paint session.</param>
        public async Task JoinSession(string sessionId)
        {

            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);

            // need to have some sort of service handle here for sending / retrieving state
            _sessionHandler.Handle(new PaintSessionProtocol.JoinPaintSession(sessionId, Context.ConnectionId, Context.UserIdentifier ?? "BadUser"));
        }

        public void AddStrokes(string sessionId, StrokeData[] strokes)
        {
            _sessionHandler.Handle(new PaintSessionProtocol.AddStrokes(sessionId, strokes));
        }
    }
}
