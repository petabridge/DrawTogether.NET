using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using DrawTogether.UI.Client.Services;

namespace DrawTogether.UI.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");


            builder.Services.AddSingleton<AkkaService>(sp =>
            {
                var actorSystem = ActorSystem.Create("BLAZOR", @"akka.loggers = []");
                return new AkkaService(actorSystem);
            });

            builder.Services.AddSingleton<IPaintSessionGenerator, GuidPaintSessionGenerator>();
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            await builder.Build().RunAsync();
        }
    }
}
