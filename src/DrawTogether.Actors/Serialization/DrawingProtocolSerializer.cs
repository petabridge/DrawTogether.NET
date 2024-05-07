using Akka.Actor;
using Akka.Serialization;
using DrawTogether.Entities.Drawings;
using DrawTogether.Entities.Drawings.Messages;

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
        throw new NotImplementedException();
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
            DrawingSessionQueries.GetDrawingSessionUsers => GetDrawingSessionUsersManifest,
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
}