using System.Threading.Channels;
using Akka.Actor;
using Akka.Event;
using Akka.Hosting;
using DrawTogether.Actors.Drawings;
using DrawTogether.Entities.Drawings;
using DrawTogether.Entities.Drawings.Messages;

namespace DrawTogether.Actors.Local;

/// <summary>
/// A local handle for a specific drawing instance
/// </summary>
public sealed class LocalDrawingSessionActor : UntypedActor, IWithTimers, IWithStash
{
    public sealed class RetryConnectionToDrawingSession : IDeadLetterSuppression, INoSerializationVerificationNeeded
    {
        private RetryConnectionToDrawingSession()
        {
        }

        public static RetryConnectionToDrawingSession Instance { get; } = new();
    }
    
    public sealed class RemoteDrawingSessionActorDied : IDeadLetterSuppression, INoSerializationVerificationNeeded
    {
        public RemoteDrawingSessionActorDied(DrawingSessionId drawingSessionId)
        {
            DrawingSessionId = drawingSessionId;
        }

        public DrawingSessionId DrawingSessionId { get; }
    }
    
    /// <summary>
    /// Used to produce a <see cref="DrawingChannelResponse"/>
    /// </summary>
    public sealed class GetDrawingChannel : IDeadLetterSuppression, INoSerializationVerificationNeeded
    {
        private GetDrawingChannel()
        {
        }
        public static GetDrawingChannel Instance { get; } = new();
    }
    
    /// <summary>
    /// Response to <see cref="GetDrawingChannel"/>
    /// </summary>
    public sealed class DrawingChannelResponse : IDeadLetterSuppression, INoSerializationVerificationNeeded
    {
        public DrawingChannelResponse(ChannelReader<IDrawingSessionEvent> drawingChannel)
        {
            DrawingChannel = drawingChannel;
        }

        public ChannelReader<IDrawingSessionEvent> DrawingChannel { get; }
    }
    
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _drawingSessionActor;
    private readonly DrawingSessionId _drawingSessionId;
    private readonly Channel<IDrawingSessionEvent> _drawingChannel = Channel.CreateUnbounded<IDrawingSessionEvent>();
    private IActorRef _debouncer = ActorRefs.Nobody;

    public LocalDrawingSessionActor(DrawingSessionId drawingSessionId, IRequiredActor<DrawingSessionActor> drawingSessionActor)
    {
        _drawingSessionId = drawingSessionId;
        _drawingSessionActor = drawingSessionActor.ActorRef;
    }

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case GetDrawingChannel:
                Sender.Tell(new DrawingChannelResponse(_drawingChannel.Reader));
                break;
            case DrawingSessionQueries.SubscribeAcknowledged subscribed:
                _log.Info("Subscribed to DrawingSession [{0}]", _drawingSessionId);
                Become(HasConnectionToDrawingSession);
                
                // used to trigger re-subcribes
                Context.WatchWith(Sender, new RemoteDrawingSessionActorDied(_drawingSessionId));
                Stash.UnstashAll();
                Timers.Cancel("ConnectToChannel");
                break;
            case RetryConnectionToDrawingSession _:
                AttemptToSubscribe();
                break;
            case IDrawingSessionEvent drawingEvent:
                // publish the event to our channel
                _drawingChannel.Writer.TryWrite(drawingEvent);
                break;
        }
    }

    private void HasConnectionToDrawingSession(object message)
    {
        
    }

    public ITimerScheduler Timers { get; set; } = null!;
    public IStash Stash { get; set; }

    protected override void PreStart()
    {
        AttemptToSubscribe();
        _drawingSessionActor.Ask<DrawingSessionState>()
    }

    private void AttemptToSubscribe()
    {
        _drawingSessionActor.Tell(new DrawingSessionQueries.SubscribeToDrawingSession(_drawingSessionId));
        Timers.StartPeriodicTimer("ConnectToChannel", RetryConnectionToDrawingSession.Instance, TimeSpan.FromMilliseconds(500));
    }
}