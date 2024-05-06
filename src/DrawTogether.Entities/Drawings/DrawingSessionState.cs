using System.Collections.Immutable;
using DrawTogether.Entities.Drawings.Messages;
using DrawTogether.Entities.Users;
using static DrawTogether.Entities.Drawings.Messages.DrawingSessionCommands;

namespace DrawTogether.Entities.Drawings;

public sealed record DrawingSessionState(DrawingSessionId DrawingSessionId) : IWithDrawingSessionId
{
    public ImmutableDictionary<StrokeId, ConnectedStroke> Strokes { get; init; } =
        ImmutableDictionary<StrokeId, ConnectedStroke>.Empty;

    public ImmutableHashSet<UserId> ConnectedUsers { get; init; } = ImmutableHashSet<UserId>.Empty;

    public DateTime LastUpdate { get; init; } = DateTime.UtcNow;
    
    public bool IsEmpty => Strokes.IsEmpty && ConnectedUsers.IsEmpty;
}

public static class DrawingSessionStateExtensions
{
    public static (CommandResult commandResult, IDrawingSessionEvent[] events) ProcessCommand(
        this DrawingSessionState state, IDrawingSessionCommand command)
    {
        switch (command)
        {
            case AddStroke addStroke:
            {
                if (state.Strokes.ContainsKey(addStroke.Stroke.Id))
                {
                    return (CommandResult.NoOp, []);
                }

                return (CommandResult.Ok,
                    new IDrawingSessionEvent[]
                        { new DrawingSessionEvents.StrokeAdded(state.DrawingSessionId, addStroke.Stroke) });
            }
            case RemoveStroke removeStroke:
            {
                if (!state.Strokes.ContainsKey(removeStroke.StrokeId))
                {
                    return (
                        new CommandResult()
                        {
                            Code = ResultCode.BadRequest,
                            Message = $"Stroke [{removeStroke.StrokeId}] does not exist and cannot be removed"
                        }, []);
                }

                return (CommandResult.Ok,
                    [new DrawingSessionEvents.StrokeRemoved(state.DrawingSessionId, removeStroke.StrokeId)]);
            }
            case ClearStrokes:
            {
                if (state.Strokes.IsEmpty)
                {
                    return (CommandResult.NoOp, []);
                }

                return (CommandResult.Ok,
                    [new DrawingSessionEvents.StrokesCleared(state.DrawingSessionId)]);
            }
            case AddUser addUser:
            {
                if (state.ConnectedUsers.Contains(addUser.UserId))
                {
                    return (CommandResult.NoOp, []);
                }

                return (CommandResult.Ok,
                    [new DrawingSessionEvents.UserAdded(state.DrawingSessionId, addUser.UserId)]);
            }
            case RemoveUser removeUser:
            {
                if (!state.ConnectedUsers.Contains(removeUser.UserId))
                {
                    return (
                        new CommandResult()
                        {
                            Code = ResultCode.BadRequest,
                            Message = $"User [{removeUser.UserId}] does not exist and cannot be removed"
                        }, []);
                }

                return (CommandResult.Ok,
                    [new DrawingSessionEvents.UserRemoved(state.DrawingSessionId, removeUser.UserId)]);
            }
            default:
            {
                return (
                    new CommandResult()
                    {
                        Code = ResultCode.BadRequest,
                        Message = $"Command [{command.GetType().Name}] is not supported"
                    }, []);
            }
        }
    }
    
    public static DrawingSessionState Apply(this DrawingSessionState currentState, IDrawingSessionEvent @event)
    {
        var e = @event switch
        {
            DrawingSessionEvents.StrokeAdded strokeAdded =>
                currentState with { Strokes = currentState.Strokes.SetItem(strokeAdded.Stroke.Id, strokeAdded.Stroke), LastUpdate = DateTime.UtcNow},
            DrawingSessionEvents.StrokeRemoved strokeRemoved =>
                currentState with { Strokes = currentState.Strokes.Remove(strokeRemoved.StrokeId) },
            DrawingSessionEvents.StrokesCleared =>
                currentState with { Strokes = ImmutableDictionary<StrokeId, ConnectedStroke>.Empty },
            DrawingSessionEvents.UserAdded userAdded =>
                currentState with { ConnectedUsers = currentState.ConnectedUsers.Add(userAdded.UserId) },
            DrawingSessionEvents.UserRemoved userRemoved =>
                currentState with { ConnectedUsers = currentState.ConnectedUsers.Remove(userRemoved.UserId) },
            _ => currentState
        };
        
        e = e with { LastUpdate = DateTime.UtcNow };
        return e;
    }
}