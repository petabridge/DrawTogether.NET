using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.Extensions.Hosting;
using static DrawTogether.UI.Shared.Connectivity.PaintSessionProtocol;

namespace DrawTogether.UI.Server.Services
{
    /// <summary>
    /// Used by SignalR to message our shared drawing system.
    /// </summary>
    public interface IDrawSessionHandler
    {
        void Handle(IPaintSessionMessage msg);
    }

    /// <summary>
    /// Runs in the background of Server process. Hosts <see cref="ActorSystem"/> responsible for powering
    /// actors that track session state data.
    /// </summary>
    public sealed class AkkaService : IHostedService, IDrawSessionHandler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHostApplicationLifetime _applicationLifetime;

        public AkkaService(IServiceProvider serviceProvider, IHostApplicationLifetime applicationLifetime)
        {
            _serviceProvider = serviceProvider;
            _applicationLifetime = applicationLifetime;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void Handle(IPaintSessionMessage msg)
        {
            throw new NotImplementedException();
        }
    }
}
