using DrawTogether.Entities.Drawings.Messages;

namespace DrawTogether.Entities.Drawings;

/// <summary>
/// Event used to signal updates about a particular drawing
/// </summary>
public sealed record DrawingActivityUpdate(DrawingSessionId DrawingSessionId, int ActiveUsers, DateTime LastUpdate, bool IsRemoved) : IWithDrawingSessionId;

public static class DrawingIndexQueries
{
    /// <summary>
    /// Get a list of all active drawing sessions
    /// </summary>
    public sealed class GetAllActiveDrawingSessions
    {
        private GetAllActiveDrawingSessions() { }
        public static GetAllActiveDrawingSessions Instance { get; } = new();
    }
}