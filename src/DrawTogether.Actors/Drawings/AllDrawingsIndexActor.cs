using Akka.Actor;
using Akka.DistributedData;
using Akka.Event;
using Akka.Hosting;
using DrawTogether.Entities.Drawings;
using static DrawTogether.Actors.ClusterConstants;

namespace DrawTogether.Actors.Drawings;

/// <summary>
///     Cluster singleton actor that keeps track of all active drawing sessions
/// </summary>
public sealed class AllDrawingsIndexActor : UntypedActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private LWWDictionary<string, DrawingActivityUpdate> _recentDrawingStateUpdates =
        LWWDictionary<string, DrawingActivityUpdate>.Empty;
    private readonly IActorRef _replicator = DistributedData.Get(Context.System).Replicator;

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            // need to handle DData messages
            case GetSuccess getSuccess: // for the initial load
            {
                var data = getSuccess.Get(AllDrawingsIndexKey);
                _recentDrawingStateUpdates = _recentDrawingStateUpdates.Merge(data);
                break;
            }
            // handle ddata updates
            case Changed changed:
            {
                var data = changed.Get(AllDrawingsIndexKey);
                _recentDrawingStateUpdates = _recentDrawingStateUpdates.Merge(data);
                break;
            }
            case DrawingIndexQueries.GetAllActiveDrawingSessions:
                var rValues = _recentDrawingStateUpdates.Values.ToList();
                Sender.Tell(_recentDrawingStateUpdates.Values.ToList());
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
    }
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


        return builder;
    }
}