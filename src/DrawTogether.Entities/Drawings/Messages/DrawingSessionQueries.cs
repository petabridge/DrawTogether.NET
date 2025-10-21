namespace DrawTogether.Entities.Drawings.Messages;

public interface IDrawingSessionQuery : IWithDrawingSessionId { }

public static class DrawingSessionQueries
{
    public sealed record GetDrawingSessionState(DrawingSessionId DrawingSessionId) : IDrawingSessionQuery;
    
    public sealed record SubscribeToDrawingSession(DrawingSessionId DrawingSessionId) : IDrawingSessionQuery;
    
    public sealed record SubscribeAcknowledged(DrawingSessionId DrawingSessionId) : IDrawingSessionQuery;
    
    public sealed record UnsubscribeFromDrawingSession(DrawingSessionId DrawingSessionId) : IDrawingSessionQuery;

    public sealed record UnsubscribeAcknowledged(DrawingSessionId DrawingSessionId) : IDrawingSessionQuery;
}