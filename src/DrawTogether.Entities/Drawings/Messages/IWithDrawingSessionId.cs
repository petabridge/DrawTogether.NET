namespace DrawTogether.Entities.Drawings.Messages;

/// <summary>
/// Marker interface for messages that have a <see cref="DrawingSessionId"/>.
/// </summary>
public interface IWithDrawingSessionId
{
    DrawingSessionId DrawingSessionId { get; }
}