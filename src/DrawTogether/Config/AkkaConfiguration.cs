using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Persistence.Sql.Config;
using Akka.Persistence.Sql.Hosting;
using Akka.Remote.Hosting;
using DrawTogether.Actors;
using DrawTogether.Actors.Drawings;
using DrawTogether.Actors.Local;
using LinqToDB;

namespace DrawTogether.Config;

public static class AkkaConfiguration
{
    public static IServiceCollection ConfigureAkka(this IServiceCollection services, IConfiguration configuration, Action<AkkaConfigurationBuilder, IServiceProvider> additionalConfig)
    {
        var akkaSettings = new AkkaSettings();
        configuration.GetSection("Akka").Bind(akkaSettings);

        services.AddSingleton(akkaSettings);
        
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (connectionString is null)
            throw new Exception("DefaultConnection setting is missing");
        
        var roleName = ClusterConstants.DrawStateRoleName;

        services.AddAkka(akkaSettings.ActorSystemName, (builder, provider) =>
        {
            builder.WithRemoting(akkaSettings.RemoteOptions)
                .WithClustering(akkaSettings.ClusterOptions)
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
}