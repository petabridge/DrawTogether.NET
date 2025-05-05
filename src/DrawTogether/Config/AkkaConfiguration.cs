using System.Diagnostics;
using Akka.Cluster.Hosting;
using Akka.Discovery.Azure;
using Akka.Discovery.Config.Hosting;
using Akka.Discovery.KubernetesApi;
using Akka.Hosting;
using Akka.Management;
using Akka.Management.Cluster.Bootstrap;
using Akka.Persistence.Sql.Config;
using Akka.Persistence.Sql.Hosting;
using Akka.Remote.Hosting;
using DrawTogether.Actors;
using DrawTogether.Actors.Drawings;
using DrawTogether.Actors.Local;
using DrawTogether.Actors.Serialization;
using LinqToDB;

namespace DrawTogether.Config;

public static class AkkaConfiguration
{
    public static IServiceCollection ConfigureAkka(this IServiceCollection services, IConfiguration configuration, Action<AkkaConfigurationBuilder, IServiceProvider> additionalConfig)
    {
        var akkaSettings = BindAkkaSettings(services, configuration);

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (connectionString is null)
            throw new Exception("DefaultConnection ConnectionString is missing");
        
        var roleName = ClusterConstants.DrawStateRoleName;

        services.AddAkka(akkaSettings.ActorSystemName, (builder, provider) =>
        {
            builder.ConfigureNetwork(provider)
                .AddDrawingProtocolSerializer()
                .WithSqlPersistence(
                    connectionString: connectionString,
                    providerName: ProviderName.SqlServer2022,
                    databaseMapping: DatabaseMapping.SqlServer,
                    tagStorageMode: TagMode.TagTable,
                    deleteCompatibilityMode: true,
                    useWriterUuidColumn: true,
                    autoInitialize: true)
                .AddAllDrawingsIndexActor(roleName)
                .AddDrawingSessionActor(roleName)
                .AddLocalDrawingSessionActor();
            
            additionalConfig(builder, provider);
        });
        
        return services;
    }

    public static AkkaSettings BindAkkaSettings(IServiceCollection services, IConfiguration configuration)
    {
        var akkaSettings = new AkkaSettings();
        configuration.GetSection(nameof(AkkaSettings)).Bind(akkaSettings);

        services.AddSingleton(akkaSettings);
        return akkaSettings;
    }

    public static AkkaConfigurationBuilder ConfigureNetwork(this AkkaConfigurationBuilder builder,
        IServiceProvider serviceProvider)
    {
        var settings = serviceProvider.GetRequiredService<AkkaSettings>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        
        builder
            .WithRemoting(settings.RemoteOptions);

        if (settings.AkkaManagementOptions is { Enabled: true })
        {
            // need to delete seed-nodes so Akka.Management will take precedence
            var clusterOptions = settings.ClusterOptions;
            clusterOptions.SeedNodes = [];

            builder
                .WithClustering(clusterOptions)
                .WithAkkaManagement(port: settings.AkkaManagementOptions.Port)
                .WithClusterBootstrap(serviceName: settings.AkkaManagementOptions.ServiceName,
                    portName: settings.AkkaManagementOptions.PortName,
                    requiredContactPoints: settings.AkkaManagementOptions.RequiredContactPointsNr);

            switch (settings.AkkaManagementOptions.DiscoveryMethod)
            {
                case DiscoveryMethod.Kubernetes:
                    builder.WithKubernetesDiscovery();
                    break;
                case DiscoveryMethod.AwsEcsTagBased:
                    break;
                case DiscoveryMethod.AwsEc2TagBased:
                    break;
                case DiscoveryMethod.AzureTableStorage:
                {
                    var connectionString = configuration.GetConnectionString("AzureStorage");
                    if (connectionString is null)
                        throw new Exception("AzureStorage ConnectionString is missing");
                    
                    builder.WithAzureDiscovery(options =>
                    {
                        options.ServiceName = settings.AkkaManagementOptions.ServiceName;
                        options.ConnectionString = connectionString;
                    });
                    break;
                }
                case DiscoveryMethod.Config:
                {
                    builder
                        .WithConfigDiscovery(options =>
                        {
                            options.Services.Add(new Service
                            {
                                Name = settings.AkkaManagementOptions.ServiceName,
                                Endpoints = new[]
                                {
                                    $"{settings.AkkaManagementOptions.Hostname}:{settings.AkkaManagementOptions.Port}",
                                }
                            });
                        });
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        else
        {
            builder.WithClustering(settings.ClusterOptions);
        }

        return builder;
    }
}