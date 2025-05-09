using Aspire.Hosting.Azure;

namespace DrawTogether.AppHost;

public static class AkkaManagementExtensions
{
    /// <summary>
    /// If our configuration specifies to use Azure Table Storage, add the table storage account
    /// </summary>
    public static IResourceBuilder<AzureTableStorageResource>? ConfigureAkkaManagementStorage(this IDistributedApplicationBuilder builder, DrawTogetherConfiguration config)
    {
        if (config.UseAkkaManagement)
        {
           
        }

        return null;
    }
    
    public static IResourceBuilder<ProjectResource> ConfigureAkkaManagementForApp(this IResourceBuilder<ProjectResource> appBuilder, DrawTogetherConfiguration config)
    {
        if (!config.UseAkkaManagement) return appBuilder;
        
        var builder = appBuilder.ApplicationBuilder;
            
        var azureStorage = builder.AddAzureStorage("storage")
            .RunAsEmulator();
        
        var tableStorage = azureStorage.AddTables("akka-discovery");

        appBuilder.WaitFor(tableStorage)
            .WithReference(tableStorage, "AkkaManagementAzure");
        
        // need to populate some config for the hosts
        appBuilder
            .WithEnvironment("AkkaSettings__AkkaManagementOptions__Enabled", "true")
            .WithEnvironment("AkkaSettings__AkkaManagementOptions__DiscoveryMethod", "AzureTableStorage")
            .WithEnvironment("AkkaSettings__AkkaManagementOptions__Port", "0"); // bind to a random port

        return appBuilder;
    }
}