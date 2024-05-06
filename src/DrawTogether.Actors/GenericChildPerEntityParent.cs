using Akka.Actor;
using Akka.Cluster.Sharding;

namespace DrawTogether.Actors;

/// <summary>
/// A generic "child per entity" parent actor.
/// </summary>
/// <remarks>
/// Intended for simplifying unit tests where we don't want to use Akka.Cluster.Sharding.
/// </remarks>
public sealed class GenericChildPerEntityParent : UntypedActor
{
    public static Props Props(IMessageExtractor extractor, Func<string, Props> propsFactory)
    {
        return Akka.Actor.Props.Create(() => new GenericChildPerEntityParent(extractor, propsFactory));
    }

    /*
     * Re-use Akka.Cluster.Sharding's infrastructure here to keep things simple.
     */
    private readonly IMessageExtractor _extractor;
    private readonly Func<string, Props> _propsFactory;

    public GenericChildPerEntityParent(IMessageExtractor extractor, Func<string, Props> propsFactory)
    {
        _extractor = extractor;
        _propsFactory = propsFactory;
    }

    protected override void OnReceive(object message)
    {
        var result = _extractor.EntityId(message);
        if (result is null)
        {
            Unhandled(message);
            return;
        }

        Context.Child(result).GetOrElse(() => Context.ActorOf(_propsFactory(result), result))
            .Forward(_extractor.EntityMessage(message));
    }
}