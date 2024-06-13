using System.Net;
using Akka.Hosting;
using Akka.Hosting.TestKit;
using DrawTogether.Config;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DrawTogether.Tests;

/// <summary>
/// We use some programming defaults, i.e. <see cref="Dns.GetHostName"/>, in some places in config.
///
/// Those values should not be used when we provide explicit values in our Microsoft.Extensions.Configuration.
/// </summary>
public class AkkaSettingConfigSanityCheck : TestKit
{
    public const string OverriddenHostName = "fakehostname";
    public const int OverriddenPort = 9112;
    
    [Fact]
    public void ShouldOverrideDefaultAkkaSettingsValues()
    {
        var settings = Host.Services.GetRequiredService<AkkaSettings>();

        settings.RemoteOptions.Port.Should().Be(OverriddenPort);
        settings.RemoteOptions.PublicHostName.Should().Be(OverriddenHostName);
    }

    protected override void ConfigureAppConfiguration(HostBuilderContext context, IConfigurationBuilder builder)
    {
        builder.AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string?>("AkkaSettings:RemoteOptions:Port", OverriddenPort.ToString()),
            new KeyValuePair<string, string?>("AkkaSettings:RemoteOptions:PublicHostName", OverriddenHostName)
        });
    }

    protected override void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        AkkaConfiguration.BindAkkaSettings(services, context.Configuration);
        base.ConfigureServices(context, services);
    }

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        
    }
}