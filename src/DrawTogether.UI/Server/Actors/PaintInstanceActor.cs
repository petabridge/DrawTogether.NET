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

        private readonly List<StrokeData> _strokes = new();
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
                    var strokeCopy = _strokes.ToArray();
                   

                    // sync a single user.
                    _hubHandler.PushStrokes(join.ConnectionId, _sessionId, strokeCopy);
                    _hubHandler.AddUsers(join.ConnectionId, _users.ToArray());

                     // let all users know about the new user
                     _users.Add(join.UserId);
                     _hubHandler.AddUser(_sessionId, join.UserId);
                    break;
                }
                case AddStrokes add:
                {
                    _strokes.AddRange(add.Strokes);
                    //_log.Info("Added {0} strokes", add.Strokes.Count);
                    // sync ALL users
                    foreach (var s in add.Strokes)
                    {
                        _streamsDebouncer.Tell(s);
                    }
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
            var (sourceRef, source) = Source.ActorRef<StrokeData>(1000, OverflowStrategy.DropHead)
                .PreMaterialize(materializer);

            _streamsDebouncer = sourceRef;
            source.GroupedWithin(10, TimeSpan.FromMilliseconds(75)).RunForeach(TransmitStrokes, materializer);

            // idle timeout all drawings after 20 minutes
            Context.SetReceiveTimeout(_idleTimeout);
        }

        private void TransmitStrokes(IEnumerable<StrokeData> strokes)
        {
            var s = strokes.ToArray();
            _log.Info("BATCHED {0} strokes", s.Length);
            _hubHandler.PushStrokes(_sessionId, s);
        }
    }
}
