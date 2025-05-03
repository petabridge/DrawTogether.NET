using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Aspire.Hosting.Testing;
using Xunit;

namespace DrawTogether.End2End.Tests;

[Collection("DrawTogether")]
public class AuthenticationTests : IAsyncLifetime
{
    private readonly DrawTogetherFixture _fixture;
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IBrowserContext _context = null!;
    private HttpClient _httpClient = null!;
    
    public AuthenticationTests(DrawTogetherFixture fixture)
    {
        _fixture = fixture;
    }
    
    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });
        _context = await _browser.NewContextAsync();
        _httpClient = new HttpClient();
    }
    
    public async Task DisposeAsync()
    {
        _httpClient.Dispose();
        await _context.DisposeAsync();
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }
    
    [Fact]
    public async Task CanAuthenticateUsingCookies()
    {
        // Get the URL of the DrawTogether web app from Aspire
        var endpoint = _fixture.App.GetEndpoint("DrawTogether");
        var endpointStr = endpoint.ToString();
        var page = await _context.NewPageAsync();
        
        // Navigate to the login page
        await page.GotoAsync($"{endpointStr}/login");
        
        // Fill and submit the login form
        await page.FillAsync("input[name='username']", "testuser");
        await page.FillAsync("input[name='password']", "testpassword");
        
        // Wait for a successful navigation after clicking the submit button
        var navigationTask = page.WaitForURLAsync($"{endpointStr}/**");
        await page.ClickAsync("button[type='submit']");
        await navigationTask;
        
        // Verify we're logged in
        var userElement = await page.QuerySelectorAsync(".user-info");
        Assert.NotNull(userElement);
        
        // Extract authentication cookies for further API tests
        var cookies = await _context.CookiesAsync();
        var authCookie = cookies.FirstOrDefault(c => c.Name == ".AspNetCore.Identity.Application");
        Assert.NotNull(authCookie);
    }
    
    [Fact]
    public async Task CanAuthenticateUsingTokens()
    {
        // Get the URL of the DrawTogether web app from Aspire
        var endpoint = _fixture.App.GetEndpoint("DrawTogether");
        var endpointStr = endpoint.ToString();
        
        // 1. Get token from API
        var response = await _httpClient.PostAsJsonAsync($"{endpointStr}/api/token", new
        {
            Username = "testuser",
            Password = "testpassword"
        });
        
        response.EnsureSuccessStatusCode();
        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(tokenResponse);
        
        // 2. Set up browser context with authentication headers
        await _context.DisposeAsync();
        _context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {tokenResponse.Token}"
            }
        });
        
        // 3. Navigate to protected page and verify access
        var page = await _context.NewPageAsync();
        await page.GotoAsync($"{endpointStr}/drawing");
        
        // 4. Verify we can access protected content
        var canvasElement = await page.QuerySelectorAsync("canvas");
        Assert.NotNull(canvasElement);
    }
    
    private class TokenResponse
    {
        public string Token { get; set; } = null!;
        public DateTimeOffset Expiration { get; set; }
    }
} 