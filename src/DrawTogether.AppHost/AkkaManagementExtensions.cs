using System.Net.Sockets;
using Aspire.Hosting.Azure;
using Microsoft.Extensions.Hosting;

namespace DrawTogether.AppHost;

public static class AkkaManagementExtensions
{
    public static IResourceBuilder<ProjectResource> ConfigureAkkaManagementForApp(this IResourceBuilder<ProjectResource> appBuilder, DrawTogetherConfiguration config)
    {
        if (!config.UseAkkaManagement) return appBuilder;
        
        var builder = appBuilder.ApplicationBuilder;
            
        var azureStorage = builder.AddAzureStorage("storage")
            .RunAsEmulator();
        
        var tableStorage = azureStorage.AddTables("akka-discovery");

        appBuilder.WaitFor(tableStorage)
            .WithReference(tableStorage, "AkkaManagementAzure");
        
        // Setup network endpoint ports
        appBuilder
            .WithEndpoint(name: "remote", protocol: ProtocolType.Tcp, env: "AkkaSettings__RemoteOptions__Port")
            .WithEndpoint(name: "management", protocol: ProtocolType.Tcp, env: "AkkaSettings__AkkaManagementOptions__Port")
            .WithEndpoint(name: "pbm", protocol: ProtocolType.Tcp, env: "AkkaSettings__PbmOptions__Port");
        
        // need to populate some config for the hosts
        appBuilder
            .WithEnvironment("AkkaSettings__RemoteOptions__PublicHostName", "localhost")
            .WithEnvironment("AkkaSettings__AkkaManagementOptions__Enabled", "true")
            .WithEnvironment("AkkaSettings__AkkaManagementOptions__DiscoveryMethod", "AzureTableStorage")
            .WithEnvironment("AkkaSettings__AkkaManagementOptions__FilterOnFallbackPort", "false");

        return appBuilder;
    }

    private static IResourceBuilder<AzureTableStorageResource>? _tableStorage;
    private static IResourceBuilder<AzureTableStorageResource> GetTableStorage(IDistributedApplicationBuilder builder)
    {
        if (_tableStorage != null) return _tableStorage;
        var azureStorage = builder.AddAzureStorage("storage");
        if(builder.Environment.IsDevelopment())
            azureStorage.RunAsEmulator();
        
        _tableStorage = azureStorage.AddTables("akka-discovery");
        return _tableStorage;
    }
    
    public static IResourceBuilder<ContainerResource> ConfigureAkkaManagementForApp(this IResourceBuilder<ContainerResource> appBuilder, DrawTogetherConfiguration config)
    {
        if (!config.UseAkkaManagement) return appBuilder;
        
        var tableStorage = GetTableStorage(appBuilder.ApplicationBuilder);

        appBuilder.WaitFor(tableStorage)
            .WithReference(tableStorage, "AkkaManagementAzure");
        
        // Setup network endpoint ports
        appBuilder
            .WithHttpEndpoint(targetPort: 8080)
            .WithEndpoint(name: "remote", protocol: ProtocolType.Tcp, targetPort: 14884, env: "AkkaSettings__RemoteOptions__Port")
            .WithEndpoint(name: "management", protocol: ProtocolType.Tcp, targetPort: 8558, env: "AkkaSettings__AkkaManagementOptions__Port")
            .WithEndpoint(name: "pbm", protocol: ProtocolType.Tcp, targetPort: 9110, env: "AkkaSettings__PbmOptions__Port");
        
        // need to populate some config for the hosts
        appBuilder
            .WithEnvironment("AkkaSettings__AkkaManagementOptions__Enabled", "true")
            .WithEnvironment("AkkaSettings__AkkaManagementOptions__DiscoveryMethod", "AzureTableStorage")
            .WithEnvironment("AkkaSettings__AkkaManagementOptions__RequiredContactPointsNr", "1")
            .WithEnvironment("AkkaSettings__AkkaManagementOptions__FilterOnFallbackPort", "false");

        return appBuilder;
    }
}