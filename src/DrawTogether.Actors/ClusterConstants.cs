using Akka.DistributedData;
using DrawTogether.Entities.Drawings;

namespace DrawTogether.Actors;

public static class ClusterConstants
{
    public const string DrawStateRoleName = "draw-state";
    
    public static readonly LWWDictionaryKey<string, DrawingActivityUpdate> AllDrawingsIndexKey =
        new("all-drawings-index");
}