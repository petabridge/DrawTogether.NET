using Microsoft.Playwright;
using Xunit.Abstractions;

namespace DrawTogether.End2End.Tests.Util;

public static class DrawTogetherPlaywrightHelpers
{
    /// <summary>
    /// The first navigation from a freshly launched Chromium can be aborted with
    /// <c>net::ERR_NETWORK_CHANGED</c>: the browser tears down in-flight requests when it
    /// observes a host network-state change, and Docker creating/removing container bridge
    /// interfaces while Aspire starts resources produces exactly that.
    /// See https://github.com/microsoft/playwright/issues/13062 (same signature, retry clears it)
    /// and https://github.com/Grizzly-Endeavors/residuum/issues/592 (Docker bridge churn).
    /// The abort happens before the page renders anything, so retrying is side-effect free.
    /// </summary>
    private const string NetworkChangedError = "ERR_NETWORK_CHANGED";

    public static async Task<IResponse?> GotoWithNetworkChangeRetryAsync(
        IPage page,
        string url,
        PageGotoOptions? options = null,
        ITestOutputHelper? output = null,
        int maxAttempts = 3)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await page.GotoAsync(url, options);
            }
            catch (PlaywrightException ex) when (
                attempt < maxAttempts &&
                ex.Message.Contains(NetworkChangedError, StringComparison.Ordinal))
            {
                output?.WriteLine(
                    $"Navigation to {url} aborted by {NetworkChangedError} " +
                    $"(attempt {attempt}/{maxAttempts}); retrying.");
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt));
            }
        }
    }

    public static async Task<ElementHandleBoundingBoxResult> LocateDrawingBox(IPage page, ITestOutputHelper output)
    {
        // Wait for the canvas to be present
        output.WriteLine("Waiting for svg element");
        // 1) (Optional) wait for the Blazor container to render
        var surfaceDiv = await page.WaitForSelectorAsync("div#paint-session", new PageWaitForSelectorOptions {
            State   = WaitForSelectorState.Attached,
            Timeout = 5_000
        });

        // 2) now wait for _its_ SVG to appear and be visible
        var drawingSurface = await page.WaitForSelectorAsync(
            "div#paint-session svg",
            new PageWaitForSelectorOptions {
                State   = WaitForSelectorState.Visible,
                Timeout = 5_000
            }
        );
            
        if (drawingSurface == null || surfaceDiv == null)
        {
            throw new Exception("svg element not found");
        }

        // Get the bounding box of the canvas
        var boundingBox = await surfaceDiv.BoundingBoxAsync();
        if (boundingBox == null)
        {
            throw new Exception("Could not get svg bounding box");
        }
            
        output.WriteLine($"Drawing area: {boundingBox.Width}×{boundingBox.Height} at {boundingBox.X},{boundingBox.Y}");

        output.WriteLine(
            $"Found canvas at position: X={boundingBox.X}, Y={boundingBox.Y}, Width={boundingBox.Width}, Height={boundingBox.Height}");
        return boundingBox;
    }
    
    public static async Task HandleNewUserRegistration(IPage page, Uri baseUri, ITestOutputHelper output)
    {
        output.WriteLine("Handling authentication");
        var authUrl = new Uri(baseUri, "/Account/Register");
        await page.GotoAsync(authUrl.ToString());
        output.WriteLine($"Navigated to {authUrl}");

        var pageFillOptions = new PageFillOptions() { Timeout = 1000 };
        
        var randomName = Guid.NewGuid().ToString();
        
        // 1) Email input
        await page.FillAsync("input[name=\"Input.Email\"]", $"{randomName}@drawtogether.io", pageFillOptions);
        
        // 2) Password input
        await page.FillAsync("input[name=\"Input.Password\"]", "DrawTogether123!", pageFillOptions);
        
        // 3) Confirm password input
        await page.FillAsync("input[name=\"Input.ConfirmPassword\"]", "DrawTogether123!", pageFillOptions);
        
        output.WriteLine("Filled out registration form");
        
        // 4) Submit button
        await page.ClickAsync("button[type=\"submit\"]");
        
        output.WriteLine("Submitted form");
        
        // Wait for UI elements that indicate successful authentication instead of URL change
        try {
            // Check for authenticated-only elements
            await page.WaitForSelectorAsync("a[href='NewPaint']", new() { Timeout = 5000 });
            
            output.WriteLine("Authentication verified by UI elements");
        } catch (Exception ex) {
            output.WriteLine($"Authentication verification failed: {ex.Message}");
            var screenshotPath = await ScreenshotHelper.SaveScreenshot(page, "auth-failure");
            output.WriteLine($"Screenshot saved to: {screenshotPath}");
            throw;
        }
    }
}