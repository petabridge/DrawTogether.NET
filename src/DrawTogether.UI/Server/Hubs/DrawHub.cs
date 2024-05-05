using DrawTogether.UI.Server.Services;
using DrawTogether.UI.Shared;
using DrawTogether.UI.Shared.Connectivity;
using Microsoft.AspNetCore.SignalR;

namespace DrawTogether.UI.Server.Hubs;

public sealed class DrawHub : Hub
{
    private readonly ILogger<DrawHub> _log;
    private readonly IDrawSessionHandler _sessionHandler;

    public DrawHub(IDrawSessionHandler sessionHandler, ILogger<DrawHub> log)
    {
        _sessionHandler = sessionHandler;
        _log = log;
    }

    /// <summary>
    ///     Connects this SignalR User to a paint session in progress.
    /// </summary>
    /// <param name="sessionId">The unique id of a specific paint session.</param>
    public async Task JoinSession(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);

        // need to have some sort of service handle here for sending / retrieving state
        _sessionHandler.Handle(new PaintSessionProtocol.JoinPaintSession(sessionId, Context.ConnectionId,
            Context.UserIdentifier ?? "BadUser"));
    }

    public void CreateConnectedStroke(string sessionId, ConnectedStroke connectedStroke)
    {
        _sessionHandler.Handle(new PaintSessionProtocol.CreateConnectedStroke(sessionId, connectedStroke));
    }

    public void AddPointToConnectedStroke(string sessionId, Guid id, Point point)
    {
        _sessionHandler.Handle(new PaintSessionProtocol.AddPointToConnectedStroke(sessionId, id, point));
    }

    public override Task OnConnectedAsync()
    {
        _log.LogInformation("Received connection from [{0}->{1}]", Context.ConnectionId, Context.UserIdentifier);
        return base.OnConnectedAsync();
    }
}