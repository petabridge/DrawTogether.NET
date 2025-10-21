using Akka.Actor;
using DrawTogether.Entities.Drawings.Messages;

namespace DrawTogether.Entities.Drawings;

/// <summary>
/// Event used to signal updates about a particular drawing
/// </summary>
public sealed record DrawingActivityUpdate(
    DrawingSessionId DrawingSessionId,
    int ActiveUsers,
    DateTime LastUpdate,
    bool IsRemoved) : IWithDrawingSessionId, IComparable<DrawingActivityUpdate>, IComparable
{
    public int CompareTo(DrawingActivityUpdate? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return LastUpdate.CompareTo(other.LastUpdate);
    }

    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj)) return 1;
        if (ReferenceEquals(this, obj)) return 0;
        return obj is DrawingActivityUpdate other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(DrawingActivityUpdate)}");
    }

    public static bool operator <(DrawingActivityUpdate? left, DrawingActivityUpdate? right)
    {
        return Comparer<DrawingActivityUpdate>.Default.Compare(left, right) < 0;
    }

    public static bool operator >(DrawingActivityUpdate? left, DrawingActivityUpdate? right)
    {
        return Comparer<DrawingActivityUpdate>.Default.Compare(left, right) > 0;
    }

    public static bool operator <=(DrawingActivityUpdate? left, DrawingActivityUpdate? right)
    {
        return Comparer<DrawingActivityUpdate>.Default.Compare(left, right) <= 0;
    }

    public static bool operator >=(DrawingActivityUpdate? left, DrawingActivityUpdate? right)
    {
        return Comparer<DrawingActivityUpdate>.Default.Compare(left, right) >= 0;
    }
}

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
    
    public sealed class SubscribeToDrawingSessionUpdates 
    {
        public SubscribeToDrawingSessionUpdates(IActorRef subscriber)
        {
            Subscriber = subscriber;
        }

        public IActorRef Subscriber { get; }
    }
    
    public sealed class UnsubscribeFromDrawingSessionUpdates
    {
        public UnsubscribeFromDrawingSessionUpdates(IActorRef subscriber)
        {
            Subscriber = subscriber;
        }

        public IActorRef Subscriber { get; }
    }
}