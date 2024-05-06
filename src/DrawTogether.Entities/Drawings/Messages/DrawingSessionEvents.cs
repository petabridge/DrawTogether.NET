using DrawTogether.Entities.Users;

namespace DrawTogether.Entities.Drawings.Messages;

public interface IDrawingSessionEvent : IWithDrawingSessionId { }

public static class DrawingSessionEvents
{
    public sealed record DrawingSessionCreated(DrawingSessionId DrawingSessionId) : IDrawingSessionEvent;
    
    public sealed record StrokeAdded(DrawingSessionId DrawingSessionId, ConnectedStroke Stroke) : IDrawingSessionEvent;
    
    public sealed record StrokeRemoved(DrawingSessionId DrawingSessionId, StrokeId StrokeId) : IDrawingSessionEvent;
    
    public sealed record StrokesCleared(DrawingSessionId DrawingSessionId) : IDrawingSessionEvent;
    
    public sealed record UserAdded(DrawingSessionId DrawingSessionId, UserId UserId) : IDrawingSessionEvent;
    
    public sealed record UserRemoved(DrawingSessionId DrawingSessionId, UserId UserId) : IDrawingSessionEvent;
    
    /// <summary>
    /// Occurs when the last user leaves the session.
    /// </summary>
    public sealed record DrawingSessionClosed(DrawingSessionId DrawingSessionId) : IDrawingSessionEvent;
}