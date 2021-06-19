using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Akka.Streams;
using Akka.Streams.Dsl;
using DrawTogether.UI.Server.Hubs;
using DrawTogether.UI.Server.Services;
using DrawTogether.UI.Shared;
using DrawTogether.UI.Shared.Connectivity;
using Microsoft.AspNetCore.SignalR;
using static DrawTogether.UI.Shared.Connectivity.PaintSessionProtocol;

namespace DrawTogether.UI.Server.Actors
{
    /// <summary>
    /// Holds all stroke data in memory for a single drawing instance.
    /// </summary>
    public class PaintInstanceActor : UntypedActor
    {
        // Handle to the Akka.NET asynchronous logging system
        private readonly ILoggingAdapter _log = Context.GetLogger();

        private readonly List<ConnectedStroke> _connectedStrokes = new();
        private readonly List<string> _users = new();
        private readonly IDrawHubHandler _hubHandler;
        private readonly string _sessionId;
        private readonly TimeSpan _idleTimeout = TimeSpan.FromMinutes(20);
        private IActorRef _streamsDebouncer;

        public PaintInstanceActor(string sessionId, IDrawHubHandler hubHandler)
        {
            _sessionId = sessionId;
            _hubHandler = hubHandler;
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case JoinPaintSession join:
                    {
                        _log.Debug("User [{0}] joined [{1}]", join.ConnectionId, join.InstanceId);
                        // need to make immutable copy of stroke data and pass it along
                        var strokeCopy = _connectedStrokes.ToArray();

                        // sync a single user.
                        _hubHandler.PushConnectedStrokes(join.ConnectionId, _sessionId, strokeCopy);
                        _hubHandler.AddUsers(join.ConnectionId, _users.ToArray());

                        // let all users know about the new user
                        _users.Add(join.UserId);
                        _hubHandler.AddUser(_sessionId, join.UserId);
                        break;
                    }
                case AddPointToConnectedStroke or CreateConnectedStroke:
                    {
                        _streamsDebouncer.Tell(message);
                        break;
                    }
                case ReceiveTimeout _:
                    {
                        _log.Info("Terminated Painting Session [{0}] after [{1}]", _sessionId, _idleTimeout);
                        Context.Stop(Self);
                        break;
                    }
                default:
                    Unhandled(message);
                    break;
            }
        }

        protected override void PreStart()
        {
            _log.Info("Started drawing session [{0}]", _sessionId);

            var materializer = Context.Materializer();
            var (sourceRef, source) = Source.ActorRef<IPaintSessionMessage>(1000, OverflowStrategy.DropHead)
                .PreMaterialize(materializer);

            _streamsDebouncer = sourceRef;
            source.GroupedWithin(10, TimeSpan.FromMilliseconds(75)).RunForeach(TransmitActions, materializer);

            // idle timeout all drawings after 20 minutes
            Context.SetReceiveTimeout(_idleTimeout);
        }

        private void TransmitActions(IEnumerable<IPaintSessionMessage> actions)
        {
            _log.Info("BATCHED {0} strokes", actions.Count());

            foreach (var action in actions)
            {
                switch (action)
                {
                    case AddPointToConnectedStroke add:
                        {
                            _connectedStrokes.Where(s => s.Id == add.Id).First().Points.Add(add.Point);
                            _log.Info("Added stroke");
                            // sync ALL users
                            // TODO: look into zero-copy for this
                            _hubHandler.AddPointToConnectedStroke(_sessionId, add.Id, add.Point);
                            break;
                        }
                    case CreateConnectedStroke create:
                        {
                            _connectedStrokes.Add(create.ConnectedStroke);
                            _log.Info("Created Connected Stroke");

                            _hubHandler.CreateNewConnectedStroke(_sessionId, create.ConnectedStroke);
                            break;
                        }
                }
            }
        }
    }
}
