using Akka.Cluster.Sharding;
using DrawTogether.Entities.Drawings.Messages;

namespace DrawTogether.Actors.Drawings;

public sealed class DrawingSessionActorMessageExtractor() : HashCodeMessageExtractor(ShardCount)
{
    public const int ShardCount = 50;

    public override string? EntityId(object message)
    {
        if(message is IWithDrawingSessionId withDrawingSessionId)
        {
            return withDrawingSessionId.DrawingSessionId.SessionId;
        }
        
        return null;
    }
}