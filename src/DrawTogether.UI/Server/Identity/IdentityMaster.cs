using System.Collections.Generic;
using Akka.Actor;
using Akka.Event;

namespace DrawTogether.UI.Server.Identity
{
    /// <summary>
    /// Parent actor that keeps track of all identity actors
    /// </summary>
    public class IdentityMaster : ReceiveActor
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly Dictionary<string, IActorRef> _usersByName;
        private readonly Dictionary<IActorRef, string> _namesByUser;

        public IdentityMaster() 
            : this(new Dictionary<string, IActorRef>(), new Dictionary<IActorRef, string>())
        {
            
        }

        public IdentityMaster(Dictionary<string, IActorRef> usersByName, Dictionary<IActorRef, string> namesByUser)
        {
            _usersByName = usersByName;
            _namesByUser = namesByUser;

            Receive<IdentityProtocol.IWithUserIdentity>(id =>
            {
                var identityActor = Context.Child(id.UserId).GetOrElse(() =>
                    {
                        var child = Context.ActorOf(Props.Create(() => new IdentityActor(id.UserId, Self)), id.UserId);
                        Context.Watch(child); // deathwatch
                        return child;
                    });
                identityActor.Forward(id);
            });

            Receive<IdentityProtocol.UserInfo>(info =>
            {
                if (info.UserName != null)
                {
                    _usersByName[info.UserName] = Sender;
                    _namesByUser[Sender] = info.UserName;
                }
                else
                {
                    _log.Warning("Received UserInfo event for user [{0}] with a NULL userName", info.UserId);
                }
            });

            Receive<IdentityProtocol.IWithUserNameOnly>(username =>
            {
                if (_usersByName.ContainsKey(username.UserName))
                {
                    _usersByName[username.UserName].Forward(username);
                }
                else // couldn't find a user with that name in our index
                {
                    Sender.Tell(IdentityProtocol.NoUser.ToAnonUser());
                }
            });

            Receive<Terminated>(t =>
            {
                if (_namesByUser.ContainsKey(t.ActorRef))
                {
                    var userName = namesByUser[t.ActorRef];
                    namesByUser.Remove(t.ActorRef);
                    _usersByName.Remove(userName);
                }
            });
        }
    }
}