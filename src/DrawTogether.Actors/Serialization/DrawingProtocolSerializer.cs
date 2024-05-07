using System.Collections.Immutable;
using Akka.Actor;
using Akka.Serialization;
using DrawTogether.Entities;
using DrawTogether.Entities.Drawings;
using DrawTogether.Entities.Drawings.Messages;
using DrawTogether.Entities.Users;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace DrawTogether.Actors.Serialization;

public sealed class DrawingProtocolSerializer : SerializerWithStringManifest
{
    // generate manifests for all classes that implement the IWithDrawingSessionId interface - keep them to 2 or 3 characters in length
    private const string StrokeAddedManifest = "sa";
    private const string StrokeRemovedManifest = "sr";
    private const string StrokesClearedManifest = "sc";
    private const string UserAddedManifest = "ua";
    private const string UserRemovedManifest = "ur";
    private const string DrawingSessionClosedManifest = "dc";
    private const string GetDrawingSessionStateManifest = "gs";
    private const string GetDrawingSessionUsersManifest = "gu";
    private const string SubscribeToDrawingSessionManifest = "su";
    private const string SubscribeAcknowledgedManifest = "sak";
    private const string UnsubscribeFromDrawingSessionManifest = "uu";
    private const string UnsubscribeAcknowledgedManifest = "uak";
    private const string AddStrokeManifest = "as";
    private const string RemoveStrokeManifest = "rs";
    private const string ClearStrokesManifest = "cs";
    private const string AddUserManifest = "au";
    private const string RemoveUserManifest = "ru";
    private const string DrawingActivityUpdateManifest = "da";
    private const string DrawingSessionStateManifest = "ds";

    public DrawingProtocolSerializer(ExtendedActorSystem system) : base(system)
    {
    }

    public override byte[] ToBinary(object obj)
    {
        switch (obj)
        {
            case IDrawingSessionEvent e:
                return e switch
                {
                    DrawingSessionEvents.StrokeAdded sa => ToProto(sa).ToByteArray(),
                    DrawingSessionEvents.StrokeRemoved sr => ToProto(sr).ToByteArray(),
                    DrawingSessionEvents.StrokesCleared sc => ToProto(sc).ToByteArray(),
                    DrawingSessionEvents.UserAdded ua => ToProto(ua).ToByteArray(),
                    DrawingSessionEvents.UserRemoved ur => ToProto(ur).ToByteArray(),
                    DrawingSessionEvents.DrawingSessionClosed dc => ToProto(dc).ToByteArray(),
                    _ => throw new ArgumentException($"Can't serialize object of type {e.GetType()}")
                };
            case IDrawingSessionCommand c:
                return c switch
                {
                    DrawingSessionCommands.AddStroke asCmd => ToProto(asCmd).ToByteArray(),
                    DrawingSessionCommands.RemoveStroke rsCmd => ToProto(rsCmd).ToByteArray(),
                    DrawingSessionCommands.ClearStrokes csCmd => ToProto(csCmd).ToByteArray(),
                    DrawingSessionCommands.AddUser auCmd => ToProto(auCmd).ToByteArray(),
                    DrawingSessionCommands.RemoveUser ruCmd => ToProto(ruCmd).ToByteArray(),
                    _ => throw new ArgumentException($"Can't serialize object of type {c.GetType()}")
                };
            case IDrawingSessionQuery q:
                return q switch
                {
                    DrawingSessionQueries.GetDrawingSessionState gs => ToProto(gs).ToByteArray(),
                    DrawingSessionQueries.SubscribeToDrawingSession su => ToProto(su).ToByteArray(),
                    DrawingSessionQueries.SubscribeAcknowledged sak => ToProto(sak).ToByteArray(),
                    DrawingSessionQueries.UnsubscribeFromDrawingSession uu => ToProto(uu).ToByteArray(),
                    DrawingSessionQueries.UnsubscribeAcknowledged uak => ToProto(uak).ToByteArray(),
                    _ => throw new ArgumentException($"Can't serialize object of type {q.GetType()}")
                };
            case DrawingActivityUpdate da:
                return ToProto(da).ToByteArray();
            case DrawingSessionState state:
                return ToProto(state).ToByteArray();
            default:
                throw new ArgumentException($"Can't serialize object of type {obj.GetType()}");
        }
    }

    private Proto.DrawingActivityUpdated ToProto(DrawingActivityUpdate asCmd)
    {
        var protoDrawingActivityUpdated = new Proto.DrawingActivityUpdated()
        {
            DrawingSessionId = asCmd.DrawingSessionId.SessionId,
            LastUpdated = asCmd.LastUpdate.ToTimestamp(),
            ActiveUsers = asCmd.ActiveUsers,
            IsRemoved = asCmd.IsRemoved
        };
        return protoDrawingActivityUpdated;
    }

    private Proto.SubscribeToDrawingSessionState ToProto(DrawingSessionQueries.SubscribeToDrawingSession asCmd)
    {
        var protoSubscribeToDrawingSessionState = new Proto.SubscribeToDrawingSessionState()
        {
            DrawingSessionId = asCmd.DrawingSessionId.SessionId
        };
        return protoSubscribeToDrawingSessionState;
    }
    
    private Proto.SubscribeAcknowledged ToProto(DrawingSessionQueries.SubscribeAcknowledged asCmd)
    {
        var protoSubscribeAcknowledged = new Proto.SubscribeAcknowledged()
        {
            DrawingSessionId = asCmd.DrawingSessionId.SessionId
        };
        return protoSubscribeAcknowledged;
    }
    
    private Proto.UnsubscribeFromDrawingSessionState ToProto(DrawingSessionQueries.UnsubscribeFromDrawingSession asCmd)
    {
        var protoUnsubscribeFromDrawingSessionState = new Proto.UnsubscribeFromDrawingSessionState()
        {
            DrawingSessionId = asCmd.DrawingSessionId.SessionId
        };
        return protoUnsubscribeFromDrawingSessionState;
    }
    
    private Proto.UnsubscribeAcknowledged ToProto(DrawingSessionQueries.UnsubscribeAcknowledged asCmd)
    {
        var protoUnsubscribeAcknowledged = new Proto.UnsubscribeAcknowledged()
        {
            DrawingSessionId = asCmd.DrawingSessionId.SessionId
        };
        return protoUnsubscribeAcknowledged;
    }
    

    private Proto.AddStroke ToProto(DrawingSessionCommands.AddStroke asCmd)
    {
        var protoAddStroke = new Proto.AddStroke()
        {
            DrawingSessionId = asCmd.DrawingSessionId.SessionId,
            ConnectedStroke = ToProto(asCmd.Stroke)
        };
        return protoAddStroke;
    }
    
    private Proto.RemoveStroke ToProto(DrawingSessionCommands.RemoveStroke rsCmd)
    {
        var protoRemoveStroke = new Proto.RemoveStroke()
        {
            DrawingSessionId = rsCmd.DrawingSessionId.SessionId,
            StrokeId = rsCmd.StrokeId.Id
        };
        return protoRemoveStroke;
    }
    
    private Proto.ClearStrokes ToProto(DrawingSessionCommands.ClearStrokes csCmd)
    {
        var protoClearStrokes = new Proto.ClearStrokes()
        {
            DrawingSessionId = csCmd.DrawingSessionId.SessionId
        };
        return protoClearStrokes;
    }
    
    private Proto.AddUser ToProto(DrawingSessionCommands.AddUser auCmd)
    {
        var protoAddUser = new Proto.AddUser()
        {
            DrawingSessionId = auCmd.DrawingSessionId.SessionId,
            UserId = auCmd.UserId.IdentityName
        };
        return protoAddUser;
    }
    
    private Proto.RemoveUser ToProto(DrawingSessionCommands.RemoveUser ruCmd)
    {
        var protoRemoveUser = new Proto.RemoveUser()
        {
            DrawingSessionId = ruCmd.DrawingSessionId.SessionId,
            UserId = ruCmd.UserId.IdentityName
        };
        return protoRemoveUser;
    }
    
    private Proto.GetDrawingSessionState ToProto(DrawingSessionQueries.GetDrawingSessionState gs)
    {
        var protoGetDrawingSessionState = new Proto.GetDrawingSessionState()
        {
            DrawingSessionId = gs.DrawingSessionId.SessionId
        };
        return protoGetDrawingSessionState;
    }
    
  
    private Proto.AddStroke ToProto(DrawingSessionEvents.StrokeAdded stroke)
    {
        var protoStrokeAdded = new Proto.AddStroke()
        {
            DrawingSessionId = stroke.DrawingSessionId.SessionId,
            ConnectedStroke = ToProto(stroke.Stroke)
        };
        return protoStrokeAdded;
    }

    private Proto.RemoveStroke ToProto(DrawingSessionEvents.StrokeRemoved stroke)
    {
        var protoStrokeRemoved = new Proto.RemoveStroke()
        {
            DrawingSessionId = stroke.DrawingSessionId.SessionId,
            StrokeId = stroke.StrokeId.Id
        };
        return protoStrokeRemoved;
    }
    
    private Proto.ClearStrokes ToProto(DrawingSessionEvents.StrokesCleared strokes)
    {
        var protoStrokesCleared = new Proto.ClearStrokes()
        {
            DrawingSessionId = strokes.DrawingSessionId.SessionId
        };
        return protoStrokesCleared;
    }
    
    private Proto.AddUser ToProto(DrawingSessionEvents.UserAdded user)
    {
        var protoUserAdded = new Proto.AddUser()
        {
            DrawingSessionId = user.DrawingSessionId.SessionId,
            UserId = user.UserId.IdentityName
        };
        return protoUserAdded;
    }
    
    private Proto.RemoveUser ToProto(DrawingSessionEvents.UserRemoved user)
    {
        var protoUserRemoved = new Proto.RemoveUser()
        {
            DrawingSessionId = user.DrawingSessionId.SessionId,
            UserId = user.UserId.IdentityName
        };
        return protoUserRemoved;
    }
    
    private Proto.SessionClosed ToProto(DrawingSessionEvents.DrawingSessionClosed closed)
    {
        var protoDrawingSessionClosed = new Proto.SessionClosed()
        {
            DrawingSessionId = closed.DrawingSessionId.SessionId
        };
        return protoDrawingSessionClosed;
    }

    public override object FromBinary(byte[] bytes, string manifest)
    {
        throw new NotImplementedException();
    }

    public override string Manifest(object o)
    {
        // return the constant string value for the manifest
        return o switch
        {
            IDrawingSessionEvent => GetManifestForEvent(o),
            IDrawingSessionCommand => GetManifestForCommand(o),
            IDrawingSessionQuery => GetManifestForQuery(o),
            DrawingSessionState => DrawingSessionStateManifest,
            _ => throw new ArgumentException($"Can't serialize object of type {o.GetType()}")
        };
    }

    private static string GetManifestForQuery(object o)
    {
        return o switch
        {
            DrawingSessionQueries.GetDrawingSessionState => GetDrawingSessionStateManifest,
            DrawingSessionQueries.SubscribeToDrawingSession => SubscribeToDrawingSessionManifest,
            DrawingSessionQueries.SubscribeAcknowledged => SubscribeAcknowledgedManifest,
            DrawingSessionQueries.UnsubscribeFromDrawingSession => UnsubscribeFromDrawingSessionManifest,
            DrawingSessionQueries.UnsubscribeAcknowledged => UnsubscribeAcknowledgedManifest,
            _ => throw new ArgumentException($"Can't serialize object of type {o.GetType()}")
        };
    }

    private static string GetManifestForCommand(object o)
    {
        return o switch
        {
            DrawingSessionCommands.AddStroke => AddStrokeManifest,
            DrawingSessionCommands.RemoveStroke => RemoveStrokeManifest,
            DrawingSessionCommands.ClearStrokes => ClearStrokesManifest,
            DrawingSessionCommands.AddUser => AddUserManifest,
            DrawingSessionCommands.RemoveUser => RemoveUserManifest,
            _ => throw new ArgumentException($"Can't serialize object of type {o.GetType()}")
        };
    }

    private static string GetManifestForEvent(object o)
    {
        return o switch
        {
            DrawingSessionEvents.StrokeAdded => StrokeAddedManifest,
            DrawingSessionEvents.StrokeRemoved => StrokeRemovedManifest,
            DrawingSessionEvents.StrokesCleared => StrokesClearedManifest,
            DrawingSessionEvents.UserAdded => UserAddedManifest,
            DrawingSessionEvents.UserRemoved => UserRemovedManifest,
            DrawingSessionEvents.DrawingSessionClosed => DrawingSessionClosedManifest,
            DrawingActivityUpdate => DrawingActivityUpdateManifest,
            _ => throw new ArgumentException($"Can't serialize object of type {o.GetType()}")
        };
    }

    Proto.ConnectedStroke ToProto(ConnectedStroke stroke)
    {
        var protoStroke = new Proto.ConnectedStroke
        {
            Id = stroke.Id.Id,
            Points = { stroke.Points.Select(p => new Proto.Point { X = p.X, Y = p.Y }) },
            StrokeWidth = stroke.StrokeWidth.Value,
            StrokeColor = stroke.StrokeColor.HexCodeOrColorName
        };
        return protoStroke;
    }

    ConnectedStroke FromProto(Proto.ConnectedStroke protoStroke)
    {
        var stroke = new ConnectedStroke(new StrokeId(protoStroke.Id))
        {
            Points = protoStroke.Points.Select(p => new Point(p.X, p.Y)).ToList(),
            StrokeWidth = new GreaterThanZeroInteger(protoStroke.StrokeWidth),
            StrokeColor = new Color(protoStroke.StrokeColor)
        };
        return stroke;
    }

    Proto.DrawingSessionState ToProto(DrawingSessionState state)
    {
        var protoState = new Proto.DrawingSessionState
        {
            DrawingSessionId = state.DrawingSessionId.SessionId,
            ConnectedStrokes = { state.Strokes.Select(v => ToProto(v.Value)) },
            ConnectedUsers = { state.ConnectedUsers.Select(u => u.IdentityName) },
            LastUpdated = state.LastUpdate.ToTimestamp()
        };
        return protoState;
    }

    DrawingSessionState FromProto(Proto.DrawingSessionState protoState)
    {
        var state = new DrawingSessionState(new DrawingSessionId(protoState.DrawingSessionId))
        {
            Strokes = protoState.ConnectedStrokes.ToImmutableDictionary(s => new StrokeId(s.Id), s => FromProto(s)),
            ConnectedUsers = protoState.ConnectedUsers.Select(u => new UserId(u)).ToImmutableHashSet(),
            LastUpdate = protoState.LastUpdated.ToDateTime()
        };
        return state;
    }
}