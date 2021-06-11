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

        public sealed class AddStrokes : IPaintSessionMessage
        {
            public AddStrokes(string instanceId, IReadOnlyList<StrokeData> strokes)
            {
                InstanceId = instanceId;
                Strokes = strokes;
            }

            public string InstanceId { get; }

            public IReadOnlyList<StrokeData> Strokes { get; }
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
