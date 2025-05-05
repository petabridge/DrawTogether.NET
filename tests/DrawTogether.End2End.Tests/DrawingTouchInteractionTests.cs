using DrawTogether.End2End.Tests.Util;
using Microsoft.Playwright;
using Xunit.Abstractions;
using static DrawTogether.End2End.Tests.Util.DrawTogetherPlaywrightHelpers;

namespace DrawTogether.End2End.Tests;

[Collection("DrawTogether")]
public class DrawingTouchInteractionTests : IAsyncLifetime
{
    private readonly DrawTogetherFixture _fixture;
    private readonly ITestOutputHelper _output;
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    
    public DrawingTouchInteractionTests(DrawTogetherFixture fixture, ITestOutputHelper output)
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
    }
    
    public async Task DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }

    private async Task<IBrowserContext> GetContext(int viewPortX, int viewPortY)
    {
        return await _browser.NewContextAsync(new BrowserNewContextOptions()
        {
            HasTouch = true,
            ViewportSize = new ViewportSize { Width = viewPortX, Height = viewPortY },
            IgnoreHTTPSErrors = true
        });
    }
    
    public static readonly TheoryData<int , int> ViewPortSizes = new()
    {
        { 375, 812 }, // iPhone X
        { 768, 1024 }, // iPad
        { 1440, 900 }, // MacBook Pro
        { 1920, 1080 } // Full HD
    };
    
    [Theory]
    [MemberData(nameof(ViewPortSizes))]
    public async Task CanDrawWithTouch(int viewPortX, int viewPortY)
    {
        // Get the URL of the DrawTogether web app from Aspire
        var endpoint = _fixture.GetDrawTogetherEndpoint();
        await using var context = await GetContext(viewPortX, viewPortY);
        var page = await context.NewPageAsync();
        
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
            
            // 5) Single tap to start drawing
            await page.Touchscreen.TapAsync(boundingBox.X + 50, boundingBox.Y + 50);
            
            // 7) Optionally assert something changed on the SVG,
            //    or just take a screenshot for manual verification:
            var screenshotPath = await ScreenshotHelper.SaveScreenshot(page, $"touch-draw-complete-{viewPortX}x{viewPortY}");
            _output.WriteLine($"Screenshot saved to: {screenshotPath}");
        }
        catch (Exception)
        {
            _output.WriteLine("An error occurred during the drawing test");
            var screenshotPath = await ScreenshotHelper.SaveScreenshot(page, $"drawing-touch-test-failure-{viewPortX}x{viewPortY}");
            _output.WriteLine($"Screenshot saved to: {screenshotPath}");
            throw;
        }
    }
} 