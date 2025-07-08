using Akka;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Streams.Stage;
using Akka.Util;
using Akka.Event;
using DrawTogether.Entities;
using DrawTogether.Entities.Drawings;
using DrawTogether.Entities.Drawings.Messages;
using DrawTogether.Entities.Users;
using System.Collections.Immutable;
using Akka.Streams.Implementation;
using static DrawTogether.Actors.Local.StrokeBuilder;

namespace DrawTogether.Actors.Local;

/// <summary>
/// A custom Akka.Streams stage that maintains continuity between strokes for each user.
/// This stage runs downstream from the GroupedWithin operator and ensures that strokes 
/// from the same continuous drawing action are connected properly without visible gaps.
/// </summary>
public class StrokeContinuityStage : GraphStage<
    FlowShape<ImmutableList<LocalPaintProtocol.IPaintSessionMessage>, IEnumerable<ConnectedStroke>>>
{
    private readonly TimeSpan _userInactivityTimeout;
    private const string UserInactivityTimeout = "user-inactivity-timeout";
    private readonly TimeSpan _strokeDisconnectTimeout = TimeSpan.FromMilliseconds(200);
    private const double StrokeDisconnectDistance = 250.0; // pixels

    // Shape definition for this stage
    public override FlowShape<ImmutableList<LocalPaintProtocol.IPaintSessionMessage>, IEnumerable<ConnectedStroke>>
        Shape { get; }

    // Constructor
    public StrokeContinuityStage(TimeSpan? inactivityTimeout = null)
    {
        _userInactivityTimeout = inactivityTimeout ?? TimeSpan.FromSeconds(30);
        var inlet = new Inlet<ImmutableList<LocalPaintProtocol.IPaintSessionMessage>>("StrokeContinuity.in");
        var outlet = new Outlet<IEnumerable<ConnectedStroke>>("StrokeContinuity.out");
        Shape =
            new FlowShape<ImmutableList<LocalPaintProtocol.IPaintSessionMessage>, IEnumerable<ConnectedStroke>>(inlet,
                outlet);
    }

    // Create logic for this stage - must be protected to match base class
    protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes) => new Logic(this);

    // Inner logic class that implements the stage behavior
    private class Logic : TimerGraphStageLogic
    {
        private readonly StrokeContinuityStage _stage;

        // State tracking for active strokes by user
        private readonly Dictionary<UserId, ActiveStrokeInfo> _activeStrokeInfo = new();

        // Class to track the last point and properties of the active stroke
        private struct ActiveStrokeInfo(Point lastPoint, Color strokeColor, GreaterThanZeroInteger strokeWidth)
        {
            public Point LastPoint { get; set; } = lastPoint;
            public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
            public Color StrokeColor { get; } = strokeColor;
            public GreaterThanZeroInteger StrokeWidth { get; } = strokeWidth;
        }

        // Random seed for generating stroke IDs
        private readonly int _randomSeed = Random.Shared.Next();
        private int _strokeIdCounter = 0;

        public Logic(StrokeContinuityStage stage) : base(stage.Shape)
        {
            _stage = stage;

            // Set up handlers for the inlet
            SetHandler(stage.Shape.Inlet, onPush: () =>
            {
                var batch = Grab(stage.Shape.Inlet);
                var strokes = ProcessBatch(batch);
                Push(stage.Shape.Outlet, strokes);
            });

            // Set up handlers for the outlet
            SetHandler(stage.Shape.Outlet, onPull: () => { Pull(stage.Shape.Inlet); });
        }

        public override void PreStart()
        {
            ScheduleRepeatedly(UserInactivityTimeout, _stage._userInactivityTimeout,
                _stage._userInactivityTimeout);
        }

        // Clean up inactive strokes
        private void CleanupInactiveStrokes()
        {
            var now = DateTime.UtcNow;
            var inactiveUsers = _activeStrokeInfo
                .Where(kv => (now - kv.Value.LastUpdate) > _stage._userInactivityTimeout)
                .Select(kv => kv.Key)
                .ToList();

            foreach (var user in inactiveUsers)
            {
                _activeStrokeInfo.Remove(user);
                Log.Info("Removed inactive stroke info for user {0}", user.IdentityName);
            }
        }

        // Generate a new stroke ID
        private StrokeId GenerateStrokeId(UserId userId)
        {
            return new StrokeId(MurmurHash.StringHash(userId.IdentityName) + _randomSeed + _strokeIdCounter++);
        }

        // Process a batch of points and maintain stroke continuity
        private IEnumerable<ConnectedStroke> ProcessBatch(ImmutableList<LocalPaintProtocol.IPaintSessionMessage> batch)
        {
            if (batch.Count == 0)
                return [];

            // Group points by user
            var pointsByUser = batch.GroupBy(p => p.UserId).ToList();
            var resultStrokes = new List<ConnectedStroke>();
            var now = DateTime.UtcNow;

            foreach (var userPoints in pointsByUser)
            {
                var userId = userPoints.Key;

                var (pointsToAdd, disconnect, strokeWidth, strokeColor) = ProcessEvents(userPoints);

                if (disconnect) // have to purge old records, if they exist
                {
                    Log.Debug("Disconnecting from previous stroke for user {0}", userId.IdentityName);
                    _activeStrokeInfo.Remove(userId);
                }

                // If no points to add, then we can exit early for this user
                if (pointsToAdd.Count == 0)
                {
                    continue; // No points to add, so skip to next user
                }

                // Always generate a new stroke ID for each batch
                var strokeId = GenerateStrokeId(userId);

                // Check if this user has an active stroke
                if (_activeStrokeInfo.TryGetValue(userId, out var activeInfo))
                {
                    // Check if stroke properties have changed
                    var strokePropertiesChanged =
                        !activeInfo.StrokeColor.Equals(strokeColor) ||
                        activeInfo.StrokeWidth.Value != strokeWidth.Value;

                    // time-difference
                    var timeSinceLastUpdate = now - activeInfo.LastUpdate;
                    var aboveThreshold = timeSinceLastUpdate > _stage._strokeDisconnectTimeout;

                    // if the properties have changed, we need to disconnect from the previous stroke
                    // we might have missed a previous StopStroke event
                    if (!strokePropertiesChanged || aboveThreshold)
                    {
                        // If the first point of this batch isn't close to the last point of the previous batch,
                        // something unusual happened (e.g., user clicked somewhere else) - no need for continuity
                        var distance = CalculateDistance(activeInfo.LastPoint, pointsToAdd[0]);

                        if (distance <= StrokeDisconnectDistance) // Threshold for what we consider "continuous"
                        {
                            // For perfect continuity, replace the first point with the last point from previous stroke
                            // This ensures there's not even a sub-pixel gap between strokes
                            pointsToAdd[0] = activeInfo.LastPoint;
                            Log.Debug("Ensured continuity for user {0} (gap was {1:F2}px)", userId.IdentityName,
                                distance);
                        }
                        else
                        {
                            Log.Debug("Gap detected for user {0} ({1:F2}px) - likely new stroke intended",
                                userId.IdentityName, distance);
                        }
                    }
                }

                // Create the stroke with its unique ID
                var connectedStroke = new ConnectedStroke(strokeId)
                {
                    Points = pointsToAdd,
                    StrokeColor = strokeColor,
                    StrokeWidth = strokeWidth
                };

                // Update the active stroke info
                if (pointsToAdd.Count > 0)
                {
                    var lastPoint = pointsToAdd[^1];
                    _activeStrokeInfo[userId] = new ActiveStrokeInfo(lastPoint, strokeColor, strokeWidth)
                    {
                        LastUpdate = now
                    };
                }

                resultStrokes.Add(connectedStroke);
                continue;

                (List<Point> points, bool disconnectFromPrevious, GreaterThanZeroInteger strokeWidth, Color strokeColor)
                    ProcessEvents(IEnumerable<LocalPaintProtocol.IPaintSessionMessage> messages)
                {
                    var points = new List<Point>();
                    var addPointStrokeWidth = GreaterThanZeroInteger.Default;
                    var addPointStrokeColor = Color.Black;
                    var disconnectFromPrevious = false;
                    foreach (var message in messages)
                    {
                        if (message is LocalPaintProtocol.AddPointToConnectedStroke addPoint)
                        {
                            addPointStrokeWidth = addPoint.StrokeWidth;
                            addPointStrokeColor = addPoint.StrokeColor;
                            points.Add(addPoint.Point);
                        }
                        else if (message is LocalPaintProtocol.StrokeCompleted)
                        {
                            disconnectFromPrevious = true;
                        }
                    }

                    return (points, disconnectFromPrevious, addPointStrokeWidth, addPointStrokeColor);
                }
            }

            Log.Info("Processed batch with {0} points from {1} users, produced {2} strokes",
                batch.Count, pointsByUser.Count, resultStrokes.Count);

            return resultStrokes;
        }

        private static double CalculateDistance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }

        protected override void OnTimer(object timerKey)
        {
            if (timerKey.Equals(UserInactivityTimeout))
            {
                CleanupInactiveStrokes();
            }
        }
    }
}

/// <summary>
/// Extension methods for working with StrokeContinuityStage
/// </summary>
public static class StrokeContinuityStageExtensions
{
    /// <summary>
    /// Adds stroke continuity tracking to a stream of batched point data
    /// </summary>
    public static Source<IEnumerable<ConnectedStroke>, TMat> WithStrokeContinuity<TMat>(
        this Source<ImmutableList<LocalPaintProtocol.IPaintSessionMessage>, TMat> source,
        TimeSpan? inactivityTimeout = null)
    {
        return source.Via(new StrokeContinuityStage(inactivityTimeout));
    }

    /// <summary>
    /// Creates a connected stroke processing flow with both GroupedWithin and stroke continuity tracking
    /// </summary>
    public static Source<IDrawingSessionCommand, TMat> ToConnectedStrokes<TMat>(
        this Source<LocalPaintProtocol.IPaintSessionMessage, TMat> source,
        DrawingSessionId drawingSessionId,
        int batchSize = 10,
        TimeSpan? batchWindow = null,
        TimeSpan? inactivityTimeout = null)
    {
        var window = batchWindow ?? TimeSpan.FromMilliseconds(75);
        var timeout = inactivityTimeout ?? TimeSpan.FromMilliseconds(Math.Max(window.TotalMilliseconds * 3, 250));

        return source
            .GroupedWithin(batchSize, window)
            .Select(items => items.ToImmutableList())
            .Via(new StrokeContinuityStage(timeout))
            .SelectMany(strokes => strokes)
            .Select(IDrawingSessionCommand (c) => new DrawingSessionCommands.AddStroke(drawingSessionId, c));
    }
}