using Akka.Actor;
using Akka.Hosting;
using DrawTogether.UI.Server.Actors;
using static DrawTogether.UI.Shared.Connectivity.PaintSessionProtocol;

namespace DrawTogether.UI.Server.Services;

/// <summary>
///     Used by SignalR to message our shared drawing system.
/// </summary>
public interface IDrawSessionHandler
{
    void Handle(IPaintSessionMessage msg);
}

public sealed class DrawSessionHandler : IDrawSessionHandler
{
    private readonly IActorRef _paintInstanceManager;

    public DrawSessionHandler(IRequiredActor<PaintInstanceManager> paintInstanceManager)
    {
        _paintInstanceManager = paintInstanceManager.GetAsync().Result;
    }

    public void Handle(IPaintSessionMessage msg)
    {
        _paintInstanceManager.Tell(msg);
    }
}