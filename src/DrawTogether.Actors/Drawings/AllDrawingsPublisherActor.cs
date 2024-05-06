using Akka.Actor;
using Akka.Cluster;
using Akka.DistributedData;
using Akka.Event;
using DrawTogether.Entities.Drawings;

namespace DrawTogether.Actors.Drawings;

/// <summary>
/// Local actor responsible for publishing all drawing session updates
/// to the <see cref="AllDrawingsIndexActor"/> for indexing
/// </summary>
public sealed class AllDrawingsPublisherActor : UntypedActor, IWithTimers
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _replicator = DistributedData.Get(Context.System).Replicator;
    private readonly IWriteConsistency _writeConsistency = new WriteMajority(TimeSpan.FromSeconds(3));

    private readonly Cluster _cluster = Cluster.Get(Context.System);
    
        
    public sealed class PruneRemovedDrawings : IDeadLetterSuppression, INoSerializationVerificationNeeded,
        INotInfluenceReceiveTimeout
    {
        private PruneRemovedDrawings()
        {
        }

        public static PruneRemovedDrawings Instance { get; } = new();
    }


    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case DrawingActivityUpdate update:
            {
                _log.Debug("Publishing drawing update for session {DrawingSessionId}", update.DrawingSessionId);
                var key = ClusterConstants.AllDrawingsIndexKey;
                var updateOp = new Update(key, LWWDictionary<string, DrawingActivityUpdate>.Empty, _writeConsistency,
                    dictionary =>
                    {
                        var dict = (LWWDictionary<string, DrawingActivityUpdate>)dictionary;
                        dict = dict.SetItem(_cluster, update.DrawingSessionId.SessionId, update);
                        return dict;
                    });
                _replicator.Tell(updateOp);
                break;
            }
            case PruneRemovedDrawings:
            {
                _log.Debug("Pruning removed drawings from index");
                var key = ClusterConstants.AllDrawingsIndexKey;
                var updateOp = new Update(key, LWWDictionary<string, DrawingActivityUpdate>.Empty, _writeConsistency,
                    dictionary =>
                    {
                        var dict = (LWWDictionary<string, DrawingActivityUpdate>)dictionary;
                        foreach (var i in dict.Entries.Where(c => c.Value.IsRemoved))
                        {
                            dict = dict.Remove(_cluster, i.Key);
                        }
                        return dict;
                    });
                _replicator.Tell(updateOp);
                break;
            }
            
            // handle response messages from the replicator
            case UpdateSuccess success:
            {
                _log.Debug("Update to drawing index successful");
                break;
            }
            case UpdateTimeout timeout:
            {
                _log.Warning(timeout.Cause, "Update to drawing index timed out");
                break;
            }
            default:
                Unhandled(message);
                break;
        }
    }

    public ITimerScheduler Timers { get; set; } = null!;

    protected override void PreStart()
    {
        // schedule a periodic update to all subscribers
        Timers.StartPeriodicTimer("prune-removed-drawings", PruneRemovedDrawings.Instance, TimeSpan.FromMinutes(30));
    }
}