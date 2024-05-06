using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.DistributedData;
using Akka.Event;
using Akka.Hosting;
using DrawTogether.Entities.Drawings;
using static DrawTogether.Actors.ClusterConstants;

namespace DrawTogether.Actors.Drawings;

/// <summary>
///     Cluster singleton actor that keeps track of all active drawing sessions
/// </summary>
public sealed class AllDrawingsIndexActor : UntypedActor, IWithTimers
{
    /// <summary>
    /// Timer-driven message to publish all updates to subscribers
    /// </summary>
    public sealed class PublishAllUpdates : IDeadLetterSuppression, INoSerializationVerificationNeeded,
        INotInfluenceReceiveTimeout
    {
        private PublishAllUpdates()
        {
        }

        public static PublishAllUpdates Instance { get; } = new();
    }
    private readonly ILoggingAdapter _log = Context.GetLogger();

    private LWWDictionary<string, DrawingActivityUpdate> _recentDrawingStateUpdates =
        LWWDictionary<string, DrawingActivityUpdate>.Empty;

    private readonly IActorRef _replicator = DistributedData.Get(Context.System).Replicator;

    private DateTime _lastUpdate = DateTime.MinValue;
    private DateTime _lastPublish = DateTime.MinValue;

    private readonly HashSet<IActorRef> _subscribers = new();

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            // need to handle DData messages
            case GetSuccess getSuccess: // for the initial load
            {
                var data = getSuccess.Get(AllDrawingsIndexKey);
                _recentDrawingStateUpdates = _recentDrawingStateUpdates.Merge(data);
                _lastUpdate = DateTime.UtcNow;
                break;
            }
            // handle ddata updates
            case Changed changed:
            {
                var data = changed.Get(AllDrawingsIndexKey);
                _recentDrawingStateUpdates = _recentDrawingStateUpdates.Merge(data);
                _lastUpdate = DateTime.UtcNow;
                break;
            }
            case DrawingIndexQueries.GetAllActiveDrawingSessions:
                var rValues = _recentDrawingStateUpdates.Values.ToList();
                Sender.Tell(rValues);
                break;
            case DrawingIndexQueries.SubscribeToDrawingSessionUpdates subscribeToDrawingSessionUpdates:
                _subscribers.Add(subscribeToDrawingSessionUpdates.Subscriber);
                Context.WatchWith(subscribeToDrawingSessionUpdates.Subscriber,
                    new DrawingIndexQueries.UnsubscribeFromDrawingSessionUpdates(subscribeToDrawingSessionUpdates
                        .Subscriber));
                break;
            case DrawingIndexQueries.UnsubscribeFromDrawingSessionUpdates unsub:
                _subscribers.Remove(unsub.Subscriber);
                Context.Unwatch(unsub.Subscriber);
                break;
            case PublishAllUpdates:
            {
                // only publish if we have new updates
                if (_lastUpdate > _lastPublish)
                {
                    _lastPublish = DateTime.UtcNow;
                    foreach (var subscriber in _subscribers)
                    {
                        subscriber.Tell(_recentDrawingStateUpdates.Values.ToList());
                    }
                }

                break;
            }
            case Terminated terminated:
                _subscribers.Remove(terminated.ActorRef);
                break;
            case NotFound _:
                // ignore - we're not initialized yet
                break;
            default:
                Unhandled(message);
                break;
        }
    }

    protected override void PreStart()
    {
        _replicator.Tell(Dsl.Get(AllDrawingsIndexKey, ReadLocal.Instance));

        // subscribe to changes in the index
        _replicator.Tell(Dsl.Subscribe(AllDrawingsIndexKey, Self));

        // schedule a periodic update to all subscribers
        Timers.StartPeriodicTimer("publish-all-updates", PublishAllUpdates.Instance, TimeSpan.FromSeconds(5));
    }

    public ITimerScheduler Timers { get; set; } = null!;
}

public static class DrawingIndexAkkaHostingExtensions
{
    public static AkkaConfigurationBuilder AddAllDrawingsIndexActor(this AkkaConfigurationBuilder builder,
        string clusterRoleName = DrawStateRoleName)
    {
        // this actor is run locally on every node for affinity reasons
        builder.WithActors((system, registry, _) =>
        {
            var allDrawingsIndexActor = system.ActorOf(Props.Create<AllDrawingsIndexActor>(), "all-drawings-index");
            registry.Register<AllDrawingsIndexActor>(allDrawingsIndexActor);
        });

        // add the corresponding publisher actor
        builder.WithActors((system, registry, resolver) =>
        {
            var allDrawingsPublisherActor =
                system.ActorOf(Props.Create<AllDrawingsPublisherActor>(), "all-drawings-publisher");
            registry.Register<AllDrawingsPublisherActor>(allDrawingsPublisherActor);
        });

        // configure DData accordingly
        builder.WithDistributedData(options =>
        {
            options.Durable = new DurableOptions()
            {
                // disable durable storage for this actor
                Keys = []
            };
            options.RecreateOnFailure = true;
            options.Role = clusterRoleName;
        });

        return builder;
    }
}