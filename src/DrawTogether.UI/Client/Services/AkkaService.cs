using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using DrawTogether.UI.Shared;
using Microsoft.AspNetCore.SignalR.Client;

namespace DrawTogether.UI.Client.Services
{
    public class AkkaService : IAsyncDisposable
    {
        private readonly ActorSystem _actorSystem;
        private readonly IMaterializer _materializer;

        public AkkaService(ActorSystem actorSystem)
        {
            _actorSystem = actorSystem;
            _materializer = _actorSystem.Materializer();
        }

        public IDebouncerService GetDebouncer(HubConnection connection, string sessionId)
        {
            return new DebouncerService(connection, _materializer, sessionId);
        }

        public async ValueTask DisposeAsync()
        {
            await _actorSystem.Terminate();
        }
    }

    /// <summary>
    /// Aggregates many client-side UI events together and
    /// pushes into the SignalR hub
    /// </summary>
    public interface IDebouncerService : IDisposable
    {
        void Push(StrokeData stroke);
    }

    public sealed class DebouncerService : IDebouncerService
    {
        private readonly HubConnection _hubConnection;
        private readonly IMaterializer _materializer;
        private readonly string _sessionId;
        private IActorRef _actor;

        public DebouncerService(HubConnection hubConnection, IMaterializer materializer, string sessionId)
        {
            _hubConnection = hubConnection;
            _materializer = materializer;
            _sessionId = sessionId;
        }

        internal void Start()
        {

            var (sourceActor, source) = Source.ActorRef<StrokeData>(5000, OverflowStrategy.DropHead)
                .PreMaterialize(_materializer);

            _actor = sourceActor;

            source.GroupedWithin(20, TimeSpan.FromMilliseconds(40))
                .RunForeach(strokes =>
                {
                    _hubConnection.SendAsync("AddStrokes", _sessionId, strokes.ToArray());
                }, _materializer);
        }

        public void Push(StrokeData stroke)
        {
            _actor.Tell(stroke);
        }

        public void Dispose()
        {
            _actor.Tell(PoisonPill.Instance);
        }
    }
}
