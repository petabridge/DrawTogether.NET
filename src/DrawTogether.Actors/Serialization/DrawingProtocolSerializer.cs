using System.Collections.Immutable;
using Akka.Actor;
using Akka.Hosting;
using Akka.Serialization;
using DrawTogether.Actors.Serialization.Proto;
using DrawTogether.Entities;
using DrawTogether.Entities.Drawings;
using DrawTogether.Entities.Drawings.Messages;
using DrawTogether.Entities.Users;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ConnectedStroke = DrawTogether.Entities.Drawings.ConnectedStroke;
using DrawingSessionState = DrawTogether.Entities.Drawings.DrawingSessionState;
using Point = DrawTogether.Entities.Drawings.Point;
using Type = System.Type;

namespace DrawTogether.Actors.Serialization;

public static class CustomSerializationAkkaExtensions
{
    public static AkkaConfigurationBuilder AddDrawingProtocolSerializer(this AkkaConfigurationBuilder builder)
    {
        return builder.WithCustomSerializer("drawing", new[] { typeof(IWithDrawingSessionId) },
            system => new DrawingProtocolSerializer(system));
    }
}

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

    // arbitrary id but not within 0-100, which is reserved by Akka.NET
    public override int Identifier => 481;

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
        return manifest switch
        {
            StrokeAddedManifest => FromProtoStrokeAdded(Proto.AddStroke.Parser.ParseFrom(bytes)),
            StrokeRemovedManifest => FromProtoStrokeRemoved(Proto.RemoveStroke.Parser.ParseFrom(bytes)),
            StrokesClearedManifest => FromProtoStrokesCleared(Proto.ClearStrokes.Parser.ParseFrom(bytes)),
            UserAddedManifest => FromProtoUserAdded(Proto.AddUser.Parser.ParseFrom(bytes)),
            UserRemovedManifest => FromProtoUserRemoved(Proto.RemoveUser.Parser.ParseFrom(bytes)),
            DrawingSessionClosedManifest => FromProtoSessionClosed(Proto.SessionClosed.Parser.ParseFrom(bytes)),
            DrawingActivityUpdateManifest => FromProto(Proto.DrawingActivityUpdated.Parser.ParseFrom(bytes)),
            GetDrawingSessionStateManifest => FromProto(Proto.GetDrawingSessionState.Parser.ParseFrom(bytes)),
            SubscribeToDrawingSessionManifest =>
                FromProto(Proto.SubscribeToDrawingSessionState.Parser.ParseFrom(bytes)),
            SubscribeAcknowledgedManifest => FromProto(Proto.SubscribeAcknowledged.Parser.ParseFrom(bytes)),
            UnsubscribeFromDrawingSessionManifest => FromProto(
                Proto.UnsubscribeFromDrawingSessionState.Parser.ParseFrom(bytes)),
            UnsubscribeAcknowledgedManifest => FromProto(Proto.UnsubscribeAcknowledged.Parser.ParseFrom(bytes)),
            AddStrokeManifest => FromProto(Proto.AddStroke.Parser.ParseFrom(bytes)),
            RemoveStrokeManifest => FromProto(Proto.RemoveStroke.Parser.ParseFrom(bytes)),
            ClearStrokesManifest => FromProto(Proto.ClearStrokes.Parser.ParseFrom(bytes)),
            AddUserManifest => FromProto(Proto.AddUser.Parser.ParseFrom(bytes)),
            RemoveUserManifest => FromProto(Proto.RemoveUser.Parser.ParseFrom(bytes)),
            DrawingSessionStateManifest => FromProto(Proto.DrawingSessionState.Parser.ParseFrom(bytes)),
            _ => throw new ArgumentException($"Can't deserialize object with manifest {manifest}")
        };
    }

    private DrawingSessionCommands.RemoveUser FromProto(RemoveUser protoStroke)
    {
        return new DrawingSessionCommands.RemoveUser(new DrawingSessionId(protoStroke.DrawingSessionId),
            new UserId(protoStroke.UserId));
    }

    private DrawingSessionCommands.AddUser FromProto(AddUser protoStroke)
    {
        return new DrawingSessionCommands.AddUser(new DrawingSessionId(protoStroke.DrawingSessionId),
            new UserId(protoStroke.UserId));
    }

    private DrawingSessionCommands.ClearStrokes FromProto(ClearStrokes protoStroke)
    {
        return new DrawingSessionCommands.ClearStrokes(new DrawingSessionId(protoStroke.DrawingSessionId));
    }

    private DrawingSessionCommands.RemoveStroke FromProto(RemoveStroke protoStroke)
    {
        return new DrawingSessionCommands.RemoveStroke(new DrawingSessionId(protoStroke.DrawingSessionId),
            new StrokeId(protoStroke.StrokeId));
    }

    private DrawingSessionCommands.AddStroke FromProto(AddStroke protoStroke)
    {
        return new DrawingSessionCommands.AddStroke(new DrawingSessionId(protoStroke.DrawingSessionId),
            FromProto(protoStroke.ConnectedStroke));
    }

    private DrawingSessionQueries.UnsubscribeAcknowledged FromProto(UnsubscribeAcknowledged protoStroke)
    {
        return new DrawingSessionQueries.UnsubscribeAcknowledged(new DrawingSessionId(protoStroke.DrawingSessionId));
    }

    private DrawingSessionQueries.UnsubscribeFromDrawingSession FromProto(
        UnsubscribeFromDrawingSessionState protoStroke)
    {
        return new DrawingSessionQueries.UnsubscribeFromDrawingSession(
            new DrawingSessionId(protoStroke.DrawingSessionId));
    }

    private DrawingSessionQueries.SubscribeAcknowledged FromProto(SubscribeAcknowledged protoStroke)
    {
        return new DrawingSessionQueries.SubscribeAcknowledged(new DrawingSessionId(protoStroke.DrawingSessionId));
    }

    private DrawingSessionQueries.SubscribeToDrawingSession FromProto(SubscribeToDrawingSessionState protoStroke)
    {
        return new DrawingSessionQueries.SubscribeToDrawingSession(new DrawingSessionId(protoStroke.DrawingSessionId));
    }

    private DrawingSessionQueries.GetDrawingSessionState FromProto(GetDrawingSessionState protoStroke)
    {
        return new DrawingSessionQueries.GetDrawingSessionState(new DrawingSessionId(protoStroke.DrawingSessionId));
    }

    private DrawingActivityUpdate FromProto(DrawingActivityUpdated protoStroke)
    {
        return new DrawingActivityUpdate(new DrawingSessionId(protoStroke.DrawingSessionId), protoStroke.ActiveUsers,
            protoStroke.LastUpdated.ToDateTime(), protoStroke.IsRemoved);
    }

    private DrawingSessionEvents.DrawingSessionClosed FromProtoSessionClosed(SessionClosed parseFrom)
    {
        return new DrawingSessionEvents.DrawingSessionClosed(new DrawingSessionId(parseFrom.DrawingSessionId));
    }

    private DrawingSessionEvents.UserRemoved FromProtoUserRemoved(RemoveUser parseFrom)
    {
        return new DrawingSessionEvents.UserRemoved(new DrawingSessionId(parseFrom.DrawingSessionId),
            new UserId(parseFrom.UserId));
    }

    private DrawingSessionEvents.UserAdded FromProtoUserAdded(AddUser parseFrom)
    {
        return new DrawingSessionEvents.UserAdded(new DrawingSessionId(parseFrom.DrawingSessionId),
            new UserId(parseFrom.UserId));
    }

    private DrawingSessionEvents.StrokesCleared FromProtoStrokesCleared(ClearStrokes parseFrom)
    {
        return new DrawingSessionEvents.StrokesCleared(new DrawingSessionId(parseFrom.DrawingSessionId));
    }

    private DrawingSessionEvents.StrokeRemoved FromProtoStrokeRemoved(RemoveStroke parseFrom)
    {
        return new DrawingSessionEvents.StrokeRemoved(new DrawingSessionId(parseFrom.DrawingSessionId),
            new StrokeId(parseFrom.StrokeId));
    }

    private DrawingSessionEvents.StrokeAdded FromProtoStrokeAdded(AddStroke parseFrom)
    {
        return new DrawingSessionEvents.StrokeAdded(new DrawingSessionId(parseFrom.DrawingSessionId),
            FromProto(parseFrom.ConnectedStroke));
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