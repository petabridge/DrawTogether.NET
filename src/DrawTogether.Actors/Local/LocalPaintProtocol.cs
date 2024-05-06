using DrawTogether.Entities.Drawings;
using DrawTogether.Entities.Drawings.Messages;
using DrawTogether.Entities.Users;

namespace DrawTogether.Actors.Local;

public static class LocalPaintProtocol
{
    public interface IPaintSessionMessage : IDrawingSessionEvent
    {
        /// <summary>
        /// User who did the thing
        /// </summary>
       UserId UserId { get; } 
    }
    
    public sealed class JoinPaintSession(DrawingSessionId drawingSessionId, UserId userId) : IPaintSessionMessage
    {
        public UserId UserId { get; } = userId;
        public DrawingSessionId DrawingSessionId { get; } = drawingSessionId;
    }
    
    public sealed class LeavePaintSession(DrawingSessionId drawingSessionId, UserId userId) : IPaintSessionMessage
    {
        public UserId UserId { get; } = userId;
        public DrawingSessionId DrawingSessionId { get; } = drawingSessionId;
    }

    public sealed class AddPointToConnectedStroke(
        Point point,
        StrokeId strokeId,
        DrawingSessionId drawingSessionId,
        UserId userId)
        : IPaintSessionMessage
    {
        public Point Point { get; } = point;

        public StrokeId StrokeId { get; } = strokeId;
        public DrawingSessionId DrawingSessionId { get; } = drawingSessionId;
        public UserId UserId { get; } = userId;
    }

    public sealed class CreateConnectedStroke(DrawingSessionId drawingSessionId, UserId userId, ConnectedStroke connectedStroke)
        : IPaintSessionMessage
    {
        public StrokeId StrokeId { get; } = connectedStroke.Id;
        
        public ConnectedStroke ConnectedStroke { get; } = connectedStroke;
        public DrawingSessionId DrawingSessionId { get; } = drawingSessionId;
        public UserId UserId { get; } = userId;
    }
}