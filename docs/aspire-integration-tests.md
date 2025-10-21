# Authoring Integration Tests with .NET Aspire + xUnit + Playwright

## 1  High-level Testing Architecture

```ascii
┌─────────────────┐                    ┌──────────────────────┐
│ xUnit test file │──uses────────────►│ DashboardServerFixture│
└─────────────────┘                    └──────────────────────┘
                                               │
                                               │ starts
                                               ▼
                                    ┌───────────────────────────┐
                                    │  DashboardWebApplication  │
                                    │  (DrawTogether)           │
                                    └───────────────────────────┘
                                               │ exposes
                                               ▼
                                  ┌──────────────────────────────┐
                                  │   Dynamic HTTP Endpoints     │
                                  └──────────────────────────────┘
                                               │ consumed by
                                               ▼
                                   ┌─────────────────────────┐
                                   │   Playwright test code  │
                                   └─────────────────────────┘
```

The **fixture** (implemented with `IAsyncLifetime`) brings up an Aspire **DistributedApplication** on random loop-back ports, then hands the discovered endpoints to Playwright so that UI tests run against a fully-working, in-memory dashboard.

---

## 2  Defining the .NET Aspire Test Fixture

Below is a distilled version of `DashboardServerFixture.cs` with explanatory comments.  Patterns marked ⚠️ indicate areas that tend to surprise newcomers.

```csharp
public sealed class DashboardServerFixture : IAsyncLifetime
{ 
    // 1. Public test-visible properties
    public DashboardWebApplication DashboardApp { get; private set; } = null!;
    public Dictionary<string,string?> Configuration { get; }
    public PlaywrightFixture PlaywrightFixture { get; }

    // 2. Constructor — build configuration and nested fixtures
    public DashboardServerFixture()
    {
        // A nested fixture circumvents xUnit's "only one generic fixture" rule ⚠️
        PlaywrightFixture = new PlaywrightFixture();

        // Bind ports dynamically via 127.0.0.1:0  ⚠️
        Configuration = new()
        {
            [DashboardConfigNames.DashboardFrontendUrlName.ConfigKey] = "http://127.0.0.1:0",
            [DashboardConfigNames.DashboardOtlpHttpUrlName.ConfigKey] = "http://127.0.0.1:0",
            [DashboardConfigNames.DashboardOtlpAuthModeName.ConfigKey] = nameof(OtlpAuthMode.Unsecured),
            [DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = nameof(FrontendAuthMode.Unsecured)
        };
    }

    // 3. IAsyncLifetime — startup
    public async ValueTask InitializeAsync()
    {
        // Start Playwright browser once per test collection
        await PlaywrightFixture.InitializeAsync();

        var aspireAssemblyDir = /* locate compiled Aspire Dashboard assembly */;

        // Merge test-time configuration with application configuration
        var config = new ConfigurationManager().AddInMemoryCollection(Configuration).Build();

        DashboardApp = new DashboardWebApplication(
            new WebApplicationOptions
            {
                ContentRootPath = aspireAssemblyDir,
                WebRootPath     = Path.Combine(aspireAssemblyDir,"wwwroot"),
                EnvironmentName = "Development",
                ApplicationName = "Aspire.Dashboard",
            },
            preConfigureBuilder: builder =>
            {
                builder.Configuration.AddConfiguration(config);
                // Replace external service calls with mocks
                builder.Services.AddSingleton<IDashboardClient, MockDashboardClient>();
            });

        await DashboardApp.StartAsync();   // ⚠️ IMPORTANT: must await fully
    }

    // 4. IAsyncLifetime — tear-down
    public async ValueTask DisposeAsync()
    {
        await DashboardApp.DisposeAsync();
        await PlaywrightFixture.DisposeAsync();
    }
}
```

### Key takeaways

* **Nested fixtures** — xUnit allows only one _generic_ fixture per collection; by embedding `PlaywrightFixture` as a field you get automatic lifetime handling without running into generic-fixture constraints.
* **Port 0 binding** (`http://127.0.0.1:0`) instructs Kestrel to pick an unused port; you'll retrieve the actual port later.
* **Configuration injection** via `ConfigurationManager` avoids hard-coding URLs.
* **Mocks at startup** — inject stub services into the DI container _before_ the app starts, eliminating external dependencies.
* **Async start/stop** — failure to `await StartAsync()` often yields cryptic "connection refused" errors when Playwright navigates too soon.

---

## 3  Surfacing Endpoints for Playwright

### 3.1  Expose Ports from `DistributedApplication`

```csharp
var frontendBinding = DashboardApp.GetEndpoint("DashboardFrontend");
var otlpBinding     = DashboardApp.GetEndpoint("OtlpIngest");

// You can now feed `frontendBinding.Url` to Playwright:
await PlaywrightFixture.Page.GotoAsync(frontendBinding.Url);
```

Implementation techniques:

* **Static extension** `GetEndpoint(this DistributedApplication, string name)` – enumerates `boundServices.Bindings` and returns host/port/path.
* **Dynamic port retrieval**  `binding.Uri` includes the port chosen by Kestrel when port 0 was used.
* **Pass via test-output**   Share the URL with Playwright through constructor injection or the fixture's public properties.

> The fixture's responsibility ends when it can say "Here is the final URL **vended by** Aspire; everything else is the UI test's job."

Never hard-code `localhost:52318` in the Playwright test itself.

---

## 4  Example xUnit Test Consuming the Fixture

```csharp
[CollectionDefinition("Dashboard collection")]
public class DashboardCollection : ICollectionFixture<DashboardServerFixture> { }

[Collection("Dashboard collection")]
public class SmokeTests
{
    private readonly DashboardServerFixture _fx;

    public SmokeTests(DashboardServerFixture fx) => _fx = fx;

    [Fact]
    public async Task DashboardLoadsHomePage()
    {
        var url = _fx.DashboardApp.GetEndpoint("DashboardFrontend").Url;
        var page = await _fx.PlaywrightFixture.Browser.NewPageAsync();
        await page.GotoAsync(url);

        await page.GetByRole(AriaRole.Heading, new() { Name = "Overview" }).WaitForAsync();
        await page.ScreenshotAsync(new() { Path = "homepage.png" });
    }
}
```

Checklist:

1. **`CollectionDefinition`** to share the fixture across tests (saves browser startup).
2. **Await page navigations** (`GotoAsync`, `WaitForAsync`) to avoid racing conditions.
3. **Screenshots** inside the CI artifact folder for debugging failures.

---

## 5  Tricky / Non-Obvious Tips

| Problem | Fix |
|---------|-----|
| Multiple instances of a fixture get created | Use `[CollectionDefinition]` and share across all test classes that require it. |
| Playwright times out immediately | Ensure the Aspire app has started: `await DashboardApp.StartAsync();` before Playwright browser navigation. |
| Tests hang on Windows with `Browser.NewPageAsync()` | Disable GPU or run headless: `Playwright.CreateAsync(new() { Channel="msedge", Headless=true });` |
| Hot reload rebuilds the dashboard mid-test | Start the `DashboardWebApplication` with `EnvironmentName="Testing"` or disable file-watcher in `WebApplicationOptions`. |
| Non-deterministic ports break CI firewall rules |  Bind to `0.0.0.0:0` to stay inside container network namespace; no external firewall trip. |

---

## 6  Minimal NuGet Package List

```xml
<ItemGroup>
  <PackageReference Include="Aspire.Hosting"           Version="*" />
  <PackageReference Include="Microsoft.Playwright"     Version="*" />
  <PackageReference Include="Microsoft.Playwright.NUnit" Version="*" />
  <PackageReference Include="xunit"                    Version="*" />
  <PackageReference Include="xunit.runner.visualstudio" Version="*" />
</ItemGroup>
```

---

## 7  CI Integration (GitHub Actions)

```yaml
- name: Install Playwright Browsers
  run: pwsh -Command "playwright install --with-deps"

- name: Run integration tests
  run: |
    dotnet test tests/Aspire.Dashboard.Tests \
      --filter Category!=Unit \
      --logger trx
```

**Important:** Run `playwright install --with-deps` _before_ `dotnet test` or browsers will be missing on the agent. 