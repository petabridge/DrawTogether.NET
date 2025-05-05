using DrawTogether.End2End.Tests.Util;
using Microsoft.Playwright;
using Xunit.Abstractions;
using static DrawTogether.End2End.Tests.Util.DrawTogetherPlaywrightHelpers;

namespace DrawTogether.End2End.Tests;

[Collection("DrawTogether")]
public class DrawingMouseInteractionTests : IAsyncLifetime
{
    private readonly DrawTogetherFixture _fixture;
    private readonly ITestOutputHelper _output;
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IBrowserContext _context = null!;
    
    public DrawingMouseInteractionTests(DrawTogetherFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }
    
    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });
        _context = await _browser.NewContextAsync(new BrowserNewContextOptions()
        {
            IgnoreHTTPSErrors = true
        });
    }
    
    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }
    
    [Fact]
    public async Task CanDrawWithMouse()
    {
        // Get the URL of the DrawTogether web app from Aspire
        var endpoint = _fixture.GetDrawTogetherEndpoint();
        var page = await _context.NewPageAsync();
        
        _output.WriteLine($"Navigating to {endpoint}");
        
        // Navigate to the app
        await page.GotoAsync(endpoint.ToString());
        
        // Handle authentication if needed - example for form-based auth
        await HandleNewUserRegistration(page, endpoint, _output);
        
        _output.WriteLine("Attempting to spawn a new drawing");
        
        // next, we need to create a new drawing
        var resp = await page.GotoAsync(
            new Uri(endpoint, "/NewPaint").ToString(),
            new PageGotoOptions {
                WaitUntil = WaitUntilState.NetworkIdle
            }
        );
        
        // response should be a redirect to a new drawing (status code 200 because: Blazor)
        Assert.NotNull(resp);
        Assert.Equal(200, resp.Status);

        try
        {

            var boundingBox = await LocateDrawingBox(page, _output);

            // Perform mouse drawing actions using page.Mouse
            // Draw a line
            _output.WriteLine("Drawing line with mouse");
            await page.Mouse.MoveAsync(boundingBox.X + 100, boundingBox.Y + 100);
            await page.Mouse.DownAsync();
            await page.Mouse.MoveAsync(boundingBox.X + 200, boundingBox.Y + 200);
            await page.Mouse.UpAsync();

            // Take a screenshot for verification
            _output.WriteLine("Taking screenshot");
            var screenshotPath = await ScreenshotHelper.SaveScreenshot(page, "drawing-test-complete");
            _output.WriteLine($"Screenshot saved to: {screenshotPath}");
        }
        catch (Exception)
        {
            _output.WriteLine("An error occurred during the drawing test");
            var screenshotPath = await ScreenshotHelper.SaveScreenshot(page, "drawing-test-failure");
            _output.WriteLine($"Screenshot saved to: {screenshotPath}");
            throw;
        }
    }
} 