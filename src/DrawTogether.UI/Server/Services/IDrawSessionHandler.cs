using System.Collections.Generic;
using System.Linq;
using static DrawTogether.UI.Shared.Connectivity.PaintSessionProtocol;

namespace DrawTogether.UI.Server.Services
{
    /// <summary>
    /// Used by SignalR to message our shared drawing system.
    /// </summary>
    public interface IDrawSessionHandler
    {
        void Handle(IPaintSessionMessage msg);
    }
}
