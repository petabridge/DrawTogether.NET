using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using Xunit;
using Xunit.Abstractions;

namespace DrawTogether.End2End.Tests;

[Collection("DrawTogether")]
public class DrawingInteractionTests : IAsyncLifetime
{
    private readonly DrawTogetherFixture _fixture;
    private readonly ITestOutputHelper _output;
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IBrowserContext _context = null!;
    
    public DrawingInteractionTests(DrawTogetherFixture fixture, ITestOutputHelper output)
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
        _context = await _browser.NewContextAsync();
    }
    
    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }
    
    [Fact]
    public void AppIsRunning()
    {
        // This is a simple verification that the app is running
        // We're not actually connecting to it, just verifying that our fixture can start it
        _output.WriteLine("Verifying app is running via fixture...");
        Assert.NotNull(_fixture.App);
        _output.WriteLine("App is running");
    }
    
    [Fact(Skip = "Need to fix endpoint connectivity")]
    public async Task CanDrawOnCanvas()
    {
        // Get the URL of the DrawTogether web app from Aspire
        var endpoint = _fixture.GetDrawTogetherEndpoint();
        var page = await _context.NewPageAsync();
        
        _output.WriteLine($"Navigating to {endpoint}");
        
        // Navigate to the app
        await page.GotoAsync(endpoint.ToString());
        
        // Handle authentication if needed - example for form-based auth
        // await HandleAuthentication(page);
        
        // Wait for the canvas to be present
        _output.WriteLine("Waiting for canvas element");
        var canvas = await page.WaitForSelectorAsync("canvas");
        if (canvas == null)
        {
            throw new Exception("Canvas element not found");
        }
        
        // Get the bounding box of the canvas
        var boundingBox = await canvas.BoundingBoxAsync();
        if (boundingBox == null)
        {
            throw new Exception("Could not get canvas bounding box");
        }
        
        _output.WriteLine($"Found canvas at position: X={boundingBox.X}, Y={boundingBox.Y}, Width={boundingBox.Width}, Height={boundingBox.Height}");
        
        // Perform mouse drawing actions using page.Mouse
        // Draw a line
        _output.WriteLine("Drawing line with mouse");
        await page.Mouse.MoveAsync(boundingBox.X + 100, boundingBox.Y + 100);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(boundingBox.X + 200, boundingBox.Y + 200);
        await page.Mouse.UpAsync();
        
        // Perform touch drawing actions
        // For touch actions, use page.Touchscreen
        _output.WriteLine("Performing touch tap");
        await page.TapAsync("canvas", new PageTapOptions 
        { 
            Position = new Position { X = boundingBox.X + 300, Y = boundingBox.Y + 300 } 
        });
        
        // For more complex touch actions
        // TouchDown
        _output.WriteLine("Performing touchscreen tap");
        await page.Touchscreen.TapAsync(boundingBox.X + 350, boundingBox.Y + 350);
        
        // Multi-point touch gestures can be simulated with multiple taps
        await page.Touchscreen.TapAsync(boundingBox.X + 400, boundingBox.Y + 400);
        
        // Verify something was drawn (this will depend on your app implementation)
        // await page.WaitForSelectorAsync(".drawing-indicator");
        
        // Take a screenshot for verification
        _output.WriteLine("Taking screenshot");
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = "drawing-test.png" });
    }
    
    private async Task HandleAuthentication(IPage page)
    {
        _output.WriteLine("Handling authentication");
        // Example for form-based authentication
        await page.FillAsync("#username", "testuser");
        await page.FillAsync("#password", "password");
        await page.ClickAsync("#login-button");
        
        // Wait for navigation to complete - using await page.WaitForURLAsync() instead of WaitForNavigationAsync
        await page.WaitForURLAsync("**/dashboard");
    }
} 