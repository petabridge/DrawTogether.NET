using Akka;
using Akka.Event;
using Akka.Streams.Dsl;
using Akka.Util;
using DrawTogether.Entities;
using DrawTogether.Entities.Drawings;
using DrawTogether.Entities.Drawings.Messages;
using DrawTogether.Entities.Users;
using System.Collections.Immutable;

namespace DrawTogether.Actors.Local;

/// <summary>
/// Used to help compute <see cref="ConnectedStroke"/>s using the commands emitted by the client.
/// </summary>
public static class StrokeBuilder
{
    public static Source<IDrawingSessionCommand, NotUsed> CreateStrokeSource(
        Source<LocalPaintProtocol.AddPointToConnectedStroke, NotUsed> inputSource, ILoggingAdapter? log,
        DrawingSessionId drawingSessionId)
    {
        return CreateStrokeSource(inputSource, log, drawingSessionId, TimeSpan.FromMilliseconds(75), 10);
    }

    public static Source<IDrawingSessionCommand, NotUsed> CreateStrokeSource(
        Source<LocalPaintProtocol.AddPointToConnectedStroke, NotUsed> inputSource, ILoggingAdapter? log, 
        DrawingSessionId drawingSessionId, TimeSpan batchWhen, int batchSize)
    {
        // need to assert that batchSize > 0 and batchWhen > 0
        if (batchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than 0");
        if (batchWhen <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(batchWhen), "Batch when must be greater than 0");
        
        // Use the extension method for a cleaner implementation
        log?.Info("Creating stroke source with batch size {0}, batch time {1}ms", batchSize, batchWhen.TotalMilliseconds);
        return inputSource.ToConnectedStrokes(drawingSessionId, batchSize, batchWhen);
    }
    
    // This method is maintained for backward compatibility with tests and other code
    public static IEnumerable<ConnectedStroke> ComputeStrokes(IReadOnlyList<LocalPaintProtocol.AddPointToConnectedStroke> actions, 
        ILoggingAdapter? log,
        Func<UserId, StrokeId> strokeIdGenerator)
    {
        // Group all the actions by user
        var userActions = actions.GroupBy(a => a.UserId).ToList();

        log?.Info("BATCHED {0} create actions from {1} users", actions.Count, userActions.Count);
        
        // Process each user's batch of actions to create strokes
        foreach (var userStuff in userActions)
        {
            var userId = userStuff.Key;
            var connectedStroke = new ConnectedStroke(strokeIdGenerator(userId))
            {
                Points = userStuff.Select(a => a.Point).ToList(),
                StrokeColor = userStuff.First().StrokeColor,
                StrokeWidth = userStuff.First().StrokeWidth
            };

            yield return connectedStroke;
        }
    }
}