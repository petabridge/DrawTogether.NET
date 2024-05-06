using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Cluster.Sharding;
using Akka.Event;
using Akka.Hosting;
using Akka.Persistence;
using DrawTogether.Entities;
using DrawTogether.Entities.Drawings;
using DrawTogether.Entities.Drawings.Messages;

namespace DrawTogether.Actors.Drawings;

/// <summary>
/// Actor that owns the source of truth for a specific drawing session
/// </summary>
public sealed class DrawingSessionActor : UntypedPersistentActor, IWithTimers
{
    /// <summary>
    /// Timer-driven message to publish the current state of the drawing session
    /// </summary>
    public sealed class PublishActivity : IDeadLetterSuppression, INoSerializationVerificationNeeded,
        INotInfluenceReceiveTimeout
    {
        private PublishActivity()
        {
        }

        public static PublishActivity Instance { get; } = new();
    }

    public override string PersistenceId { get; }
    public DrawingSessionState State { get; private set; }

    /// <summary>
    /// Used for updating the <see cref="AllDrawingsIndexActor"/>'s state and
    /// operates are a lower data refresh rate than our direct subscribers
    /// </summary>
    private readonly IActorRef _drawingActivityPublisher;

    private DateTime? _lastActivityPublished;

    /// <summary>
    /// Subscribers to this drawing session - they will be notified
    /// if we are killed / rebalanced re-subscribe.
    /// </summary>
    private readonly HashSet<IActorRef> _subscribers = new();

    private readonly ILoggingAdapter _log = Context.GetLogger();

    public DrawingSessionActor(string sessionId, IRequiredActor<AllDrawingsPublisherActor> drawingActivityPublisher)
    {
        PersistenceId = sessionId;
        _drawingActivityPublisher = drawingActivityPublisher.ActorRef;
        State = new DrawingSessionState(new DrawingSessionId(sessionId));
    }

    protected override void OnCommand(object message)
    {
        switch (message)
        {
            case IDrawingSessionCommand cmd:
            {
                var (resp, events) = State.ProcessCommand(cmd);

                if (resp.IsError || events.Length == 0)
                {
                    Sender.Tell(resp);
                    return;
                }

                var hasReplied = false;

                if (State.IsEmpty)
                {
                    // special case: state is empty, need to speed up activity publishing
                    Self.Tell(PublishActivity.Instance);
                }

                PersistAll(events, evt =>
                {
                    if (!hasReplied)
                    {
                        Sender.Tell(resp);
                        hasReplied = true;
                    }

                    PublishToSubscribers(evt);
                    State = State.Apply(evt);


                    if (LastSequenceNr % 100 == 0)
                    {
                        SaveSnapshot(State);
                    }
                });

                break;
            }
            case PublishActivity:
            {
                if (_lastActivityPublished.HasValue && _lastActivityPublished == State.LastUpdate)
                {
                    // we've already published this update
                    return;
                }

                _drawingActivityPublisher.Tell(new DrawingActivityUpdate(State.DrawingSessionId,
                    State.ConnectedUsers.Count, State.LastUpdate, false));

                // sync the clocks
                _lastActivityPublished = State.LastUpdate;
                break;
            }
            case DrawingSessionQueries.SubscribeToDrawingSession _:
            {
                _subscribers.Add(Sender);
                Context.Watch(Sender);
                Sender.Tell(new DrawingSessionQueries.SubscribeAcknowledged(State.DrawingSessionId));
                break;
            }
            case DrawingSessionQueries.UnsubscribeFromDrawingSession _:
            {
                _subscribers.Remove(Sender);
                Sender.Tell(new DrawingSessionQueries.UnsubscribeAcknowledged(State.DrawingSessionId));
                break;
            }
            case Terminated terminated:
            {
                _subscribers.Remove(terminated.ActorRef);
                break;
            }
            case DrawingSessionQueries.GetDrawingSessionState:
            {
                Sender.Tell(State);
                break;
            }
            case ReceiveTimeout _:
            {
                _log.Info("Drawing session {DrawingSessionId} has been idle for too long, closing session");

                Persist(new DrawingSessionEvents.DrawingSessionClosed(State.DrawingSessionId), evt =>
                {
                    State = State.Apply(evt);
                    PublishToSubscribers(evt);

                    // let everyone know the session is toast
                    _drawingActivityPublisher.Tell(new DrawingActivityUpdate(State.DrawingSessionId,
                        State.ConnectedUsers.Count, State.LastUpdate, true));

                    // have the sharding system forget us and shut us down
                    Context.Parent.Tell(new Passivate(PoisonPill.Instance));
                });
                break;
            }
        }
    }

    private void PublishToSubscribers(IDrawingSessionEvent evt)
    {
        foreach (var subscriber in _subscribers)
        {
            subscriber.Tell(evt);
        }
    }

    protected override void OnRecover(object message)
    {
        switch (message)
        {
            case SnapshotOffer { Snapshot: DrawingSessionState state }:
                State = state;
                break;
            case IDrawingSessionEvent evt:
                State = State.Apply(evt);
                break;
        }
    }

    protected override void PreStart()
    {
        Timers.StartPeriodicTimer("publish-activity", PublishActivity.Instance, TimeSpan.FromSeconds(5));

        // if we go more than 2 minutes without activity, we time the session out
        Context.SetReceiveTimeout(TimeSpan.FromMinutes(2));
    }

    public ITimerScheduler Timers { get; set; } = null!;
}

public static class DrawingSessionActorExtensions
{
    public static AkkaConfigurationBuilder AddDrawingSessionActor(this AkkaConfigurationBuilder builder, string clusterRoleName = ClusterConstants.DrawStateRoleName)
    {
        builder.WithShardRegion<DrawingSessionActor>("drawing-session",
            (system, registry, resolver) => s => resolver.Props<DrawingSessionActor>(s),
            new DrawingSessionActorMessageExtractor(), new ShardOptions()
            {
                StateStoreMode = StateStoreMode.DData,
                RememberEntities = true,
                RememberEntitiesStore = RememberEntitiesStore.Eventsourced,
                Role = clusterRoleName
            });
        return builder;
    }
}