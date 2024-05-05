using Akka.Actor;

namespace DrawTogether.UI.Server.Actors;

/// <summary>
///     Gives a user a randomly-assigned name and associates that
///     with their session on DrawTogether
/// </summary>
public class UserInstanceActor : UntypedActor
{
    protected override void OnReceive(object message)
    {
    }
}