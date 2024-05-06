using System.Threading.Channels;
using Akka.Actor;
using Akka.Event;
using Akka.Hosting;
using Akka.Streams;
using Akka.Streams.Dsl;
using DrawTogether.Actors.Drawings;
using DrawTogether.Entities;
using DrawTogether.Entities.Drawings;
using DrawTogether.Entities.Drawings.Messages;
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
        public DrawingChannelResponse(ChannelReader<IDrawingSessionEvent> drawingChannel)
        {
            DrawingChannel = drawingChannel;
        }

        public ChannelReader<IDrawingSessionEvent> DrawingChannel { get; }
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

    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _drawingSessionActor;
    private readonly DrawingSessionId _drawingSessionId;
    private readonly Channel<IDrawingSessionEvent> _drawingChannel = Channel.CreateUnbounded<IDrawingSessionEvent>();
    private readonly IMaterializer _materializer = Context.System.Materializer();
    private IActorRef _debouncer = ActorRefs.Nobody;

    public LocalDrawingSessionActor(DrawingSessionId drawingSessionId,
        IRequiredActor<DrawingSessionActor> drawingSessionActor)
    {
        _drawingSessionId = drawingSessionId;
        _drawingSessionActor = drawingSessionActor.ActorRef;
    }

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case AddPointToConnectedStroke or CreateConnectedStroke:
                _debouncer.Tell(message);
                break;
            case IPaintSessionMessage paintMessage:
                _drawingSessionActor.Tell(paintMessage);
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
                _drawingChannel.Writer.TryWrite(drawingEvent);
                break;
            case DrawingSessionQueries.GetDrawingSessionState:
                // forward message to the ShardRegion directly
                _drawingSessionActor.Forward(message);
                break;
            case GetDrawingChannel:
                Sender.Tell(new DrawingChannelResponse(_drawingChannel.Reader));
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
            case ReceiveTimeout _:
                _log.Warning("Shutting down local handle to [{0}]", _drawingSessionId);
                Context.Stop(Self);
                break;
            case RemoteDrawingSessionActorDied died:
                _log.Warning("Remote DrawingSession [{0}] died", died.DrawingSessionId);
                AttemptToSubscribe();
                break;
        }
    }

    private List<IDrawingSessionCommand> TransmitActions(IReadOnlyList<IPaintSessionMessage> actions)
    {
        var createActions = actions.Where(a => a is CreateConnectedStroke)
            .Select(a => ((CreateConnectedStroke)a).ConnectedStroke)
            .ToArray();

        var addActions = actions.OfType<AddPointToConnectedStroke>().ToArray();

        _log.Info("BATCHED {0} create actions and {1} add actions", createActions.Length, addActions.Length);

        var drawSessionCommands = new List<IDrawingSessionCommand>();

        foreach (var create in createActions)
        {
            var allPoints = create.Points.ToList();

            foreach (var a in addActions.Where(a => a.StrokeId == create.Id))
            {
                allPoints.Add(a.Point);
            }

            drawSessionCommands.Add(new DrawingSessionCommands.AddStroke(_drawingSessionId,
                create with { Points = allPoints }));
        }

        return drawSessionCommands;
    }

    public ITimerScheduler Timers { get; set; } = null!;
    public IStash Stash { get; set; } = null!;

    protected override void PreStart()
    {
        AttemptToSubscribe();

        Context.SetReceiveTimeout(TimeSpan.FromMinutes(2));
        var (sourceRef, source) = Source.ActorRef<IPaintSessionMessage>(1000, OverflowStrategy.DropHead)
            .PreMaterialize(_materializer);

        _debouncer = sourceRef;
        source.GroupedWithin(10, TimeSpan.FromMilliseconds(75))
            .Select(c => TransmitActions(c.ToList()))
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

    protected override void PostStop()
    {
        _drawingChannel.Writer.TryComplete();
    }

    private void AttemptToSubscribe()
    {
        _drawingSessionActor.Tell(new DrawingSessionQueries.SubscribeToDrawingSession(_drawingSessionId));
        Timers.StartPeriodicTimer("ConnectToChannel", RetryConnectionToDrawingSession.Instance,
            TimeSpan.FromMilliseconds(500));
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