using Akka.Event;
using DrawTogether.Entities.Drawings;
using DrawTogether.Entities.Users;

namespace DrawTogether.Actors.Local;

/// <summary>
/// Used to help compute <see cref="ConnectedStroke"/>s using the commands emitted by the client.
/// </summary>
public static class StrokeBuilder
{
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