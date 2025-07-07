using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace DrawTogether.End2End.Tests;

public class DrawTogetherFixture : IAsyncLifetime
{
    private DistributedApplication? _app;
    
    public DistributedApplication App => _app ?? throw new InvalidOperationException("Application not initialized");
    
    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.DrawTogether_AppHost>([
            "DrawTogether:UseVolumes=false",
            "DrawTogether:UseAkkaManagement=false",
            "DrawTogether:Replicas=1"
        ]);

        _app = await builder.BuildAsync();
        
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        await _app.StartAsync(cts.Token);
        
        // Wait for the DrawTogether web app to be ready
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("DrawTogether", cts.Token);
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }
    
    // Get the endpoint for the DrawTogether web app
    public Uri GetDrawTogetherEndpoint()
    {
        if (_app == null)
            throw new InvalidOperationException("Application not initialized");
            
        // Get the endpoint URL using Aspire API
        var endpoint = _app.GetEndpoint("DrawTogether");
        if (endpoint == null)
            throw new InvalidOperationException("Endpoint not found for DrawTogether resource");
            
        return endpoint;
    }
}

// Define a collection fixture for sharing the DrawTogetherFixture
[CollectionDefinition("DrawTogether")]
public class DrawTogetherCollection : ICollectionFixture<DrawTogetherFixture>
{
    // This class has no code, and is never created. Its purpose is to be 
    // the place to apply [CollectionDefinition] and all the ICollectionFixture<> interfaces.
}