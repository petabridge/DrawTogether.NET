using System.Collections.Immutable;
using Akka.Hosting;
using Akka.Hosting.TestKit;
using Akka.Serialization;
using DrawTogether.Actors.Serialization;
using DrawTogether.Entities;
using DrawTogether.Entities.Drawings;
using DrawTogether.Entities.Users;
using FluentAssertions;

namespace DrawTogether.Tests.Serialization;

public class DrawingProtocolSerializerSpecs : TestKit
{
    // generate a test case for serializing each type that implements IWithDrawingSessionId
    [Fact]
    public void ShouldSerializeDrawingSessionState()
    {
        var drawingSessionState = new DrawingSessionState(new DrawingSessionId("iD1"))
        {
            ConnectedUsers = ImmutableHashSet<UserId>.Empty.Add(new UserId("user1")).Add(new UserId("user2")),
            Strokes = ImmutableDictionary<StrokeId, ConnectedStroke>.Empty.Add(new StrokeId(1),
                new ConnectedStroke(new StrokeId(1))
                {
                    StrokeWidth = new GreaterThanZeroInteger(4), StrokeColor = new Color("white"), Points = new[]
                    {
                        new Point(2, 3),
                        new Point(2, 2)
                    }
                })
        };
        
        VerifySerialization(drawingSessionState);
    }

    private void VerifySerialization<TMessage>(TMessage message)
    {
        var serializerFor = (SerializerWithStringManifest)Sys.Serialization.FindSerializerFor(message);
        serializerFor.Should().BeOfType<DrawingProtocolSerializer>();

        var manifest = serializerFor.Manifest(message);
        var bytes = serializerFor.ToBinary(message);

        var deserialized = (TMessage)serializerFor.FromBinary(bytes, manifest);
        deserialized.Should().BeEquivalentTo(message);
    }

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        builder.AddDrawingProtocolSerializer();
    }
}