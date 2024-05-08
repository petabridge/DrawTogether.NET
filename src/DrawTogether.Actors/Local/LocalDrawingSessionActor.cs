using System.Threading.Channels;
using Akka;
using Akka.Actor;
using Akka.Event;
using Akka.Hosting;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Util;
using DrawTogether.Actors.Drawings;
using DrawTogether.Entities;
using DrawTogether.Entities.Drawings;
using DrawTogether.Entities.Drawings.Messages;
using DrawTogether.Entities.Users;
using static DrawTogether.Actors.Local.LocalPaintProtocol;

namespace DrawTogether.Actors.Local;

/// <summary>
/// A local handle for a specific drawing instance
/// </summary>
public sealed class LocalDrawingSessionActor : UntypedActor, IWithTimers
{
    public sealed class RetryConnectionToDrawingSession : IDeadLetterSuppression, INoSerializationVerificationNeeded,
        INotInfluenceReceiveTimeout
    {
        private RetryConnectionToDrawingSession()
        {
        }

        public static RetryConnectionToDrawingSession Instance { get; } = new();
    }

    public sealed class RemoteDrawingSessionActorDied : IDeadLetterSuppression, INoSerializationVerificationNeeded
    {
        public RemoteDrawingSessionActorDied(DrawingSessionId drawingSessionId)
        {
            DrawingSessionId = drawingSessionId;
        }

        public DrawingSessionId DrawingSessionId { get; }
    }
    
    public sealed class LocalDrawingSessionSubscriberDied(IActorRef localDrawingSessionActor)
        : IDeadLetterSuppression, INoSerializationVerificationNeeded
    {
        public IActorRef LocalDrawingSessionActor { get; } = localDrawingSessionActor;
    }

    /// <summary>
    /// Used to produce a <see cref="DrawingChannelResponse"/>
    /// </summary>
    public sealed class GetDrawingChannel : IDeadLetterSuppression, INoSerializationVerificationNeeded,
        IWithDrawingSessionId
    {
        public GetDrawingChannel(DrawingSessionId drawingSessionId)
        {
            DrawingSessionId = drawingSessionId;
        }

        public DrawingSessionId DrawingSessionId { get; }
    }

    /// <summary>
    /// Response to <see cref="GetDrawingChannel"/>
    /// </summary>
    public sealed class DrawingChannelResponse : IDeadLetterSuppression, INoSerializationVerificationNeeded
    {
        public DrawingChannelResponse(ChannelReader<IDrawingSessionEvent> drawingChannel, CancellationTokenSource doneReading)
        {
            DrawingChannel = drawingChannel;
            DoneReading = doneReading;
        }

        public ChannelReader<IDrawingSessionEvent> DrawingChannel { get; }
        
        public CancellationTokenSource DoneReading { get; }
    }
    
    public sealed class GetLocalActorHandle : IDeadLetterSuppression, INoSerializationVerificationNeeded,
        IWithDrawingSessionId
    {
        public GetLocalActorHandle(DrawingSessionId drawingSessionId)
        {
            DrawingSessionId = drawingSessionId;
        }

        public DrawingSessionId DrawingSessionId { get; }
    }

    private class LocalSubscriberDied : IDeadLetterSuppression, INoSerializationVerificationNeeded
    {
        public LocalSubscriberDied(IActorRef subscriber)
        {
            Subscriber = subscriber;
        }

        public IActorRef Subscriber { get; }
    }

    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _drawingSessionActor;
    private readonly DrawingSessionId _drawingSessionId;
    private readonly Dictionary<IActorRef, CancellationTokenSource> _clientSessions = new();
    private readonly IMaterializer _materializer = Context.Materializer();
    private IActorRef _debouncer = ActorRefs.Nobody;

    public LocalDrawingSessionActor(string drawingSessionId,
        IRequiredActor<DrawingSessionActor> drawingSessionActor)
    {
        _drawingSessionId = new DrawingSessionId(drawingSessionId);
        _drawingSessionActor = drawingSessionActor.ActorRef;
    }

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case AddPointToConnectedStroke:
                _debouncer.Tell(message);
                break;
            case JoinPaintSession paintMessage:
                _drawingSessionActor.Tell(new DrawingSessionCommands.AddUser(_drawingSessionId, paintMessage.UserId));
                break;
            case LeavePaintSession paintMessage:
                _drawingSessionActor.Tell(new DrawingSessionCommands.RemoveUser(_drawingSessionId, paintMessage.UserId));
                break;
            case ClearDrawingSession:
                _drawingSessionActor.Tell(new DrawingSessionCommands.ClearStrokes(_drawingSessionId));
                break;
            case CommandResult cmdResult:
                if (cmdResult.IsError)
                {
                    _log.Warning("Failed to send commands to DrawingSession [{0}]: {1}", _drawingSessionId,
                        cmdResult.Message);
                }
                break;
            case IDrawingSessionEvent drawingEvent: // live updates from the server (source of truth)
                // publish the event to our channel
                PublishEvent(drawingEvent);
                break;
            case DrawingSessionQueries.GetDrawingSessionState:
                // forward message to the ShardRegion directly
                _drawingSessionActor.Forward(message);
                break;
            case GetDrawingChannel:
                var cts = new CancellationTokenSource();
                var self = Self; // closure
                var (sourceRef, src) = Source.ActorRef<IDrawingSessionEvent>(1000, OverflowStrategy.DropHead)
                    .PreMaterialize(_materializer);
                var (channel, channelSink) = ChannelSink.AsReader<IDrawingSessionEvent>(100, true, BoundedChannelFullMode.Wait)
                    .PreMaterialize(_materializer);
                src.Via(cts.Token.AsFlow<IDrawingSessionEvent>()) // lets the client kill the channel
                    .WatchTermination((mat, task) =>
                    {
                        // ReSharper disable once MethodSupportsCancellation
                        task.ContinueWith(_ =>
                        {
                            self.Tell(new LocalDrawingSessionSubscriberDied(sourceRef));
                            return NotUsed.Instance;
                        });
                        return mat;
                    })
                    .To(channelSink).Run(_materializer);
                Context.WatchWith(sourceRef, new LocalSubscriberDied(sourceRef));
                _clientSessions[sourceRef] = cts;
                Sender.Tell(new DrawingChannelResponse(channel, cts));
                break;
            case DrawingSessionQueries.SubscribeAcknowledged subscribed:
                _log.Info("Subscribed to DrawingSession [{0}]", _drawingSessionId);
                // used to trigger re-subcribes
                Context.WatchWith(Sender, new RemoteDrawingSessionActorDied(_drawingSessionId));
                Timers.Cancel("ConnectToChannel");
                break;
            case GetLocalActorHandle:
                Sender.Tell(Self);
                break;
            case RetryConnectionToDrawingSession _:
                AttemptToSubscribe();
                break;
            case LocalSubscriberDied died:
                _log.Debug("Local subscriber [{0}] died, removing from list", died.Subscriber);
                if (_clientSessions.TryGetValue(died.Subscriber, out var cancelSub))
                {
                    cancelSub.Cancel(); // ensure that the stream stops
                    _clientSessions.Remove(died.Subscriber);
                }
                break;
            case ReceiveTimeout _:
                // if we have any connected clients, keep the session open
                if (_clientSessions.Count == 0)
                {
                    _log.Warning("Shutting down local handle to [{0}]", _drawingSessionId);
                    Context.Stop(Self);
                }
                break;
            case RemoteDrawingSessionActorDied died:
                _log.Warning("Remote DrawingSession [{0}] died", died.DrawingSessionId);
                AttemptToSubscribe();
                break;
        }
    }
    
    private readonly int _randomSeed = Random.Shared.Next();
    private int _strokeIdCounter = 0;

    private int NextStrokeId(UserId userId)
    {
        return MurmurHash.StringHash(userId.IdentityName) + _randomSeed + _strokeIdCounter++;
    }
    
    private List<IDrawingSessionCommand> TransmitActions(IReadOnlyList<AddPointToConnectedStroke> actions)
    {
        // group all the actions by user
        var userActions = actions.GroupBy(a => a.UserId).ToList();

        _log.Info("BATCHED {0} create actions from {1} users", actions.Count, userActions.Count);

        var drawSessionCommands = new List<IDrawingSessionCommand>();

        foreach (var userStuff in userActions)
        {
            var userId = userStuff.Key;
            var connectedStroke = new ConnectedStroke(new StrokeId(NextStrokeId(userId)))
            {
                Points = userStuff.Select(a => a.Point).ToList(),
                StrokeColor = userStuff.First().StrokeColor,
                StrokeWidth = userStuff.First().StrokeWidth
            };
            
            drawSessionCommands.Add(new DrawingSessionCommands.AddStroke(_drawingSessionId, connectedStroke));
        }

        return drawSessionCommands;
    }
    
    private void PublishEvent(IDrawingSessionEvent drawingEvent)
    {
        foreach(var client in _clientSessions)
            client.Key.Tell(drawingEvent);
    }

    public ITimerScheduler Timers { get; set; } = null!;

    protected override void PreStart()
    {
        AttemptToSubscribe();

        Context.SetReceiveTimeout(TimeSpan.FromMinutes(20));
        var (sourceRef, source) = Source.ActorRef<AddPointToConnectedStroke>(1000, OverflowStrategy.DropHead)
            .PreMaterialize(_materializer);

        _debouncer = sourceRef;
        source.GroupedWithin(10, TimeSpan.FromMilliseconds(75))
            .Select(c => TransmitActions(c.ToList()))
            .SelectMany(c => c)
            .SelectAsync(1, async c =>
            {
                try
                {
                    var result = await _drawingSessionActor.Ask<CommandResult>(c, TimeSpan.FromSeconds(2));

                    return result;
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Failed to send commands to DrawingSession [{0}]", _drawingSessionId);
                    return new CommandResult()
                        { Code = ResultCode.TimeOut, Message = "Failed to send commands to DrawingSession" };
                }
            })
            .RunForeach(result =>
            {
                if (result.IsError)
                {
                    _log.Warning("Failed to send commands to DrawingSession [{0}]: {1}", _drawingSessionId,
                        result.Message);
                }
            }, _materializer);
    }

    private void AttemptToSubscribe()
    {
        _drawingSessionActor.Tell(new DrawingSessionQueries.SubscribeToDrawingSession(_drawingSessionId));
        Timers.StartPeriodicTimer("ConnectToChannel", RetryConnectionToDrawingSession.Instance,
            TimeSpan.FromMilliseconds(500));
    }
    
    protected override void PostStop()
    {
        foreach(var client in _clientSessions)
            client.Value.Cancel();
        _clientSessions.Clear();
    }
}

public static class LocalDrawingActorConfigExtensions
{
    public static AkkaConfigurationBuilder AddLocalDrawingSessionActor(this AkkaConfigurationBuilder builder)
    {
        builder.WithActors((system, registry, resolver) =>
        {
            var genericChildPerEntityParentProps = Props.Create(() => new GenericChildPerEntityParent(
                new DrawingSessionActorMessageExtractor(),
                id => resolver.Props<LocalDrawingSessionActor>(id)));

            var genericChildPerEntityParent = system.ActorOf(genericChildPerEntityParentProps, "local-drawing-session");
            registry.Register<LocalDrawingSessionActor>(genericChildPerEntityParent);
        });
        return builder;
    }
}