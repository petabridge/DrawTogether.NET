using System.Collections.Immutable;
using DrawTogether.Entities.Drawings.Messages;
using DrawTogether.Entities.Users;

namespace DrawTogether.Entities.Drawings;

public sealed record DrawingSessionState(DrawingSessionId DrawingSessionId) : IWithDrawingSessionId
{
    public ImmutableDictionary<StrokeId, ConnectedStroke> Strokes { get; init; } = ImmutableDictionary<StrokeId, ConnectedStroke>.Empty;
    
    public ImmutableHashSet<UserId> ConnectedUsers { get; init; } = ImmutableHashSet<UserId>.Empty;
    
    public DateTime LastUpdate { get; init; } = DateTime.UtcNow;
}