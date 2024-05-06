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
public sealed class AllDrawingsPublisherActor : UntypedActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _replicator = DistributedData.Get(Context.System).Replicator;
    private readonly IWriteConsistency _writeConsistency = new WriteMajority(TimeSpan.FromSeconds(3));

    private readonly Cluster _cluster = Cluster.Get(Context.System);

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case DrawingActivityUpdate { IsRemoved: false } update:
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
            case DrawingActivityUpdate { IsRemoved: true } update:
            {
                _log.Debug("Removing drawing session {DrawingSessionId} from index", update.DrawingSessionId);
                var key = ClusterConstants.AllDrawingsIndexKey;
                var removeOp = new Update(key, LWWDictionary<string, DrawingActivityUpdate>.Empty, _writeConsistency,
                    dictionary =>
                    {
                        var dict = (LWWDictionary<string, DrawingActivityUpdate>)dictionary;
                        
                        // purge this entry from the replicated copies of the dictionaries
                        dict = dict.Remove(_cluster, update.DrawingSessionId.SessionId);
                        return dict;
                    });
                _replicator.Tell(removeOp);
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
}