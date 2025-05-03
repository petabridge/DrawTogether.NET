using Akka;
using Akka.Event;
using Akka.Streams.Dsl;
using Akka.Util;
using DrawTogether.Entities.Drawings;
using DrawTogether.Entities.Drawings.Messages;
using DrawTogether.Entities.Users;

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
        Source<LocalPaintProtocol.AddPointToConnectedStroke, NotUsed> inputSource, ILoggingAdapter? log, DrawingSessionId drawingSessionId, TimeSpan batchWhen, int batchSize)
    {
        // need to assert that batchSize > 0 and batchWhen > 0
        if (batchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than 0");
        if (batchWhen <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(batchWhen), "Batch when must be greater than 0");
        
        var randomSeed = Random.Shared.Next();
        var strokeIdCounter = 0;
        
        Func<UserId, StrokeId> nextStrokeId = userId =>
        { 
            var id = new StrokeId(MurmurHash.StringHash(userId.IdentityName) + randomSeed + strokeIdCounter++);
            return id;
        };

        return inputSource.GroupedWithin(batchSize, batchWhen)
            .Select(c => ComputeStrokes(c.ToList(), log, nextStrokeId))
            .SelectMany(c => c)
            .Select(IDrawingSessionCommand (c) => new DrawingSessionCommands.AddStroke(drawingSessionId, c));
    }
    
    public static IEnumerable<ConnectedStroke> ComputeStrokes(IReadOnlyList<LocalPaintProtocol.AddPointToConnectedStroke> actions, 
        ILoggingAdapter? log,
        Func<UserId, StrokeId> strokeIdGenerator)
    {
        // group all the actions by user
        var userActions = actions.GroupBy(a => a.UserId).ToList();

        log?.Info("BATCHED {0} create actions from {1} users", actions.Count, userActions.Count);
        
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