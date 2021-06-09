// -----------------------------------------------------------------------
// <copyright file="AkkaService.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2021 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.DependencyInjection;
using DrawTogether.UI.Server.Actors;
using DrawTogether.UI.Shared.Connectivity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DrawTogether.UI.Server.Services
{
    /// <summary>
    /// INTERNAL API
    /// </summary>
    public static class AkkaExtensions
    {
        public static void AddAkka(this IServiceCollection services)
        {
            // creates an instance of the ISignalRProcessor that can be handled by SignalR
            services.AddSingleton<IDrawSessionHandler, AkkaService>();

            // starts the IHostedService, which creates the ActorSystem and actors
            services.AddHostedService<AkkaService>(sp => (AkkaService)sp.GetRequiredService<IDrawSessionHandler>());
        }
    }

    /// <summary>
    /// Runs in the background of Server process. Hosts <see cref="ActorSystem"/> responsible for powering
    /// actors that track session state data.
    /// </summary>
    public sealed class AkkaService : IHostedService, IDrawSessionHandler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHostApplicationLifetime _applicationLifetime;

        private ActorSystem _system;
        private IActorRef _paintManager;

        public AkkaService(IServiceProvider serviceProvider, IHostApplicationLifetime applicationLifetime)
        {
            _serviceProvider = serviceProvider;
            _applicationLifetime = applicationLifetime;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // need this for Akka.NET DI
            var spSetup = ServiceProviderSetup.Create(_serviceProvider);

            // need this for HOCON (when we actually need it)
            var bootstrapSetup = BootstrapSetup.Create();

            var actorSystemSetup = spSetup.And(bootstrapSetup);

            _system = ActorSystem.Create("PaintSys", actorSystemSetup);
            _paintManager = _system.ActorOf(Props.Create(() => new PaintInstanceManager()), "paint");

            // mutually assured destruction
            _system.WhenTerminated.ContinueWith(tr =>
            {
                _applicationLifetime.StopApplication();
            });

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // force ActorSystem to shut down
            await _system.Terminate();
        }

        public void Handle(PaintSessionProtocol.IPaintSessionMessage msg)
        {
            _paintManager.Tell(msg);
        }
    }
}