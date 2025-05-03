using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace DrawTogether.End2End.Tests;

public class DrawTogetherFixture : IAsyncLifetime
{
    private DistributedApplication? _app;
    
    public DistributedApplication App => _app ?? throw new InvalidOperationException("Application not initialized");
    
    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.DrawTogether_AppHost>();
        _app = await builder.BuildAsync();
        
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("DrawTogether", cts.Token);
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }
}