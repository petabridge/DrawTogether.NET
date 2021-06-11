using System;
using System.Collections.Generic;
using Akka.Actor;

namespace DrawTogether.UI.Shared.Connectivity
{
    public static class PaintSessionProtocol
    {
        public interface IPaintSessionMessage
        {
            /// <summary>
            /// Refers to a unique painting instance.
            /// </summary>
            string InstanceId { get; }
        }

        public sealed class JoinPaintSession : IPaintSessionMessage
        {
            public JoinPaintSession(string instanceId, string connectionId, string userId)
            {
                InstanceId = instanceId;
                ConnectionId = connectionId;
                UserId = userId;
            }

            public string InstanceId { get; }

            /// <summary>
            /// The unique connection id for this websocket in SignalR
            /// </summary>
            public string ConnectionId { get; }

            public string UserId { get; }
        }

        public sealed class AddPointsToConnectedStroke : IPaintSessionMessage
        {
            public AddPointsToConnectedStroke(string instanceId, Guid id, Point[] points)
            {
                InstanceId = instanceId;
                Id = id;
                Points = points;
            }

            public string InstanceId { get; }

            public Guid Id { get; }

            public IReadOnlyList<Point> Points { get; }
        }

        public sealed class CreateConnectedStroke : IPaintSessionMessage
        {
            public CreateConnectedStroke(string instanceId, ConnectedStroke connectedStroke)
            {
                InstanceId = instanceId;
                ConnectedStroke = connectedStroke;
            }

            public string InstanceId { get; }

            public ConnectedStroke ConnectedStroke { get; }
        }

        //public sealed class SubscribeToSession : IPaintSessionMessage
        //{
        //    public SubscribeToSession(string instanceId, IActorRef subscriber)
        //    {
        //        InstanceId = instanceId;
        //        Subscriber = subscriber;
        //    }

        //    public string InstanceId { get; }

        //    public IActorRef Subscriber { get; }
        //}

        //public sealed class UnsubscribeFromSession : IPaintSessionMessage
        //{
        //    public UnsubscribeFromSession(string instanceId, IActorRef subscriber)
        //    {
        //        InstanceId = instanceId;
        //        Subscriber = subscriber;
        //    }

        //    public string InstanceId { get; }

        //    public IActorRef Subscriber { get; }
        //}
    }
}
