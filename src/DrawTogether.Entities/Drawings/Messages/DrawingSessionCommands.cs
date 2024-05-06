using DrawTogether.Entities.Users;

namespace DrawTogether.Entities.Drawings.Messages;

public interface IDrawingSessionCommand : IWithDrawingSessionId{ }

public static class DrawingSessionCommands
{
    public sealed record AddStroke(DrawingSessionId DrawingSessionId, ConnectedStroke Stroke) : IDrawingSessionCommand;
    
    public sealed record RemoveStroke(DrawingSessionId DrawingSessionId, StrokeId StrokeId) : IDrawingSessionCommand;
    
    public sealed record ClearStrokes(DrawingSessionId DrawingSessionId) : IDrawingSessionCommand;
    
    public sealed record AddUser(DrawingSessionId DrawingSessionId, UserId UserId) : IDrawingSessionCommand;
    
    public sealed record RemoveUser(DrawingSessionId DrawingSessionId, UserId UserId) : IDrawingSessionCommand;
}