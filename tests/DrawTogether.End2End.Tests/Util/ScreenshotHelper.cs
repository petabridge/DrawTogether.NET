using Microsoft.Playwright;

namespace DrawTogether.End2End.Tests;

/// <summary>
/// Static helper class for saving screenshots in Playwright tests.
/// </summary>
public static class ScreenshotHelper
{
    public static async Task<string> SaveScreenshot(IPage page, string screenshotName)
    {
        // Create screenshots directory if it doesn't exist
        var screenshotDir = Path.Combine(Directory.GetCurrentDirectory(), "TestScreenshots");
        Directory.CreateDirectory(screenshotDir);
        
        // Generate filename with timestamp to avoid overwrites
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var filename = $"{screenshotName}_{timestamp}.png";
        var fullPath = Path.Combine(screenshotDir, filename);
        
        // Save the screenshot - explicitly specify PNG format
        await page.ScreenshotAsync(new PageScreenshotOptions 
        { 
            Path = fullPath,
            Type = ScreenshotType.Png 
        });
        
        return fullPath;
    }
}