using Akka.Actor;
using Akka.Persistence;
using DrawTogether.Entities.Drawings;

namespace DrawTogether.Actors.Drawings;

public sealed class DrawingSessionActor : UntypedPersistentActor
{
    public DrawingSessionActor(string sessionId)
    {
        PersistenceId = sessionId;
        State = new DrawingSessionState(new DrawingSessionId(sessionId));
    }

    public override string PersistenceId { get; }
    
    public DrawingSessionState State { get; private set; }
    
    protected override void OnCommand(object message)
    {
        throw new NotImplementedException();
    }

    protected override void OnRecover(object message)
    {
        throw new NotImplementedException();
    }
}