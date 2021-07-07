using Akka.Actor;

namespace DrawTogether.UI.Server.Services
{
    public interface IIdentityActorService
    {
        public IActorRef IdentityActor
        {
            get;
        }
    }
}