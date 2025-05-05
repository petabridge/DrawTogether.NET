using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using Xunit;
using Xunit.Abstractions;
using System.IO;
using System.Net;

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
    
    [Fact()]
    public async Task CanDrawOnCanvas()
    {
        // Get the URL of the DrawTogether web app from Aspire
        var endpoint = _fixture.GetDrawTogetherEndpoint();
        var page = await _context.NewPageAsync();
        
        _output.WriteLine($"Navigating to {endpoint}");
        
        // Navigate to the app
        await page.GotoAsync(endpoint.ToString());
        
        // Handle authentication if needed - example for form-based auth
        await HandleNewUserRegistration(page, endpoint);
        
        _output.WriteLine("Attempting to spawn a new drawing");
        
        // next, we need to create a new drawing
        var resp = await page.GotoAsync(new Uri(endpoint, "/NewDrawing").ToString());
        
        // response should be a redirect to a new drawing
        Assert.NotNull(resp);
        Assert.Equal((int)HttpStatusCode.Redirect, resp.Status);

        try
        {

            // Wait for the canvas to be present
            _output.WriteLine("Waiting for svg element");
            var drawingSurface = await page.WaitForSelectorAsync("svg", new PageWaitForSelectorOptions()
            {
                Timeout = 5000,
            });
            
            if (drawingSurface == null)
            {
                throw new Exception("Canvas element not found");
            }

            // Get the bounding box of the canvas
            var boundingBox = await drawingSurface.BoundingBoxAsync();
            if (boundingBox == null)
            {
                throw new Exception("Could not get canvas bounding box");
            }

            _output.WriteLine(
                $"Found canvas at position: X={boundingBox.X}, Y={boundingBox.Y}, Width={boundingBox.Width}, Height={boundingBox.Height}");

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
    
    private async Task HandleNewUserRegistration(IPage page, Uri baseUri)
    {
        _output.WriteLine("Handling authentication");
        var authUrl = new Uri(baseUri, "/Account/Register");
        await page.GotoAsync(authUrl.ToString());
        _output.WriteLine($"Navigated to {authUrl}");

        var pageFillOptions = new PageFillOptions() { Timeout = 1000 };
        
        var randomName = Guid.NewGuid().ToString();
        
        // 1) Email input
        await page.FillAsync("input[name=\"Input.Email\"]", $"{randomName}@drawtogether.io", pageFillOptions);
        
        // 2) Password input
        await page.FillAsync("input[name=\"Input.Password\"]", "DrawTogether123!", pageFillOptions);
        
        // 3) Confirm password input
        await page.FillAsync("input[name=\"Input.ConfirmPassword\"]", "DrawTogether123!", pageFillOptions);
        
        _output.WriteLine("Filled out registration form");
        
        // 4) Submit button
        await page.ClickAsync("button[type=\"submit\"]");
        
        _output.WriteLine("Submitted form");
        
        // Wait for UI elements that indicate successful authentication instead of URL change
        try {
            // Check for authenticated-only elements
            await page.WaitForSelectorAsync("a[href='NewPaint']", new() { Timeout = 5000 });
            
            _output.WriteLine("Authentication verified by UI elements");
        } catch (Exception ex) {
            _output.WriteLine($"Authentication verification failed: {ex.Message}");
            var screenshotPath = await ScreenshotHelper.SaveScreenshot(page, "auth-failure");
            _output.WriteLine($"Screenshot saved to: {screenshotPath}");
            throw;
        }
    }
} 