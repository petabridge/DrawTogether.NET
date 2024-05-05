// -----------------------------------------------------------------------
// <copyright file="AkkaService.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2021 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using Akka.Hosting;
using DrawTogether.UI.Server.Actors;

namespace DrawTogether.UI.Server.Services;

/// <summary>
///     INTERNAL API
/// </summary>
public static class AkkaExtensions
{
    public static void AddDrawTogetherAkka(this IServiceCollection services)
    {
        // creates an instance of the ISignalRProcessor that can be handled by SignalR
        services.AddAkka("DrawTogether", (builder, sp) =>
        {
            builder.WithActors((system, registry, resolver) =>
            {
                var paintActor = system.ActorOf(Props.Create(() => new PaintInstanceManager()), "paint");
                registry.Register<PaintInstanceManager>(paintActor);
            });
        });
    }
}