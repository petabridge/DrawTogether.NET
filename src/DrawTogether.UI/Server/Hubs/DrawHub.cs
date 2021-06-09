using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace DrawTogether.UI.Server.Hubs
{
    public sealed class DrawHub : Hub
    {
        /// <summary>
        /// Connects this SignalR User to a paint session in progress.
        /// </summary>
        /// <param name="sessionId">The unique id of a specific paint session.</param>
        public void JoinSession(string sessionId)
        {
            // need to have some sort of service handle here for sending / retrieving state
        }
    }
}
