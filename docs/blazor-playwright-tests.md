# Using Playwright to Drive a Blazor Application

This guide uses the .NET Aspire Dashboard (Blazor Server) as a concrete example, but the patterns apply to **any Blazor WebAssembly or Server** app.

---

## 1  Bootstrapping Playwright

```csharp
var playwright = await Playwright.CreateAsync();
var browser    = await playwright.Chromium.LaunchAsync(
                    new() { Headless = true });
var page       = await browser.NewPageAsync();
```

For deterministic CI runs, pin the browser channel:

```csharp
Chromium.LaunchAsync(new() { Channel = "msedge", Headless=true });
```

---

## 2  Navigation in Blazor Apps

### 2.1  Initial Page Load

Blazor renders _one_ HTML document; subsequent page changes happen via JavaScript.  
So the **first** navigation is classic:

```csharp
await page.GotoAsync("https://localhost:5001");
```

### 2.2  In-App Route Changes (3 common methods)

| Goal | Code | Notes |
|------|------|-------|
| Click an internal nav link | `await page.GetByRole(AriaRole.Link,new(){Name="Settings"}).ClickAsync();` | Wait for **render** rather than network idle, because no page reload occurs. |
| Programmatic navigation (via JS) | `await page.EvaluateAsync("Blazor.navigateTo('/settings')");` | Works in Blazor ≥ 8.0 (`Blazor` object exposed globally). |
| Hard refresh to deep URL | `await page.GotoAsync(baseUrl + "/settings");` | Blazor supports deep linking, so full load is fine. |

### 2.3  Wait Strategies

After a Blazor internal navigation the URL changes instantly, but DOM may take a frame to update.  
Prefer:

```csharp
await page.WaitForSelectorAsync("h1:has-text('Settings')");
```

over `WaitForLoadStateAsync`, because the network remains idle.

---

## 3  Authentication Inside a Blazor Application

### 3.1  Interactive Login UI

```csharp
await page.GotoAsync(baseUrl + "/login");

await page.FillAsync("input[name='username']", "alice");
await page.FillAsync("input[name='password']", "P@ssw0rd");
await page.ClickAsync("button[type='submit']");

// Wait for redirect to dashboard
await page.WaitForURLAsync("**/dashboard");
```

### 3.2  Programmatic Cookie Injection (faster)

```csharp
await page.Context.AddCookiesAsync(new[]
{
    new Cookie
    {
        Name  = ".AspNetCore.Cookies",
        Value = "encryptedCookieValue",
        Url   = baseUrl
    }
});
await page.GotoAsync(baseUrl);  // Already authenticated
```

Generate `encryptedCookieValue` via API endpoint or an in-memory auth handler in the test server.

### 3.3  Microsoft Entra / OAuth Flows

* Stub the identity provider locally with **WireMock** or **Duende IdentityServer**.
* Use Playwright **route interception** to short-circuit the redirect and return a valid token.

---

## 4  Clicking Elements & Touch Events

```csharp
// Standard click — pointer
await page.GetByText("Add Widget").ClickAsync();

// Touchscreen tap (mobile emulation)
await page.EmulateMediaAsync(new() { Media = Media.Screen });
await page.Touchscreen.TapAsync(150, 300);
```

Extra patterns:

* **Right-click**  
  `await page.ClickAsync(selector, new() { Button = MouseButton.Right });`
* **Hover->Click inside dropdown**  
  ```csharp
  var menu = page.Locator("#profileMenu");
  await menu.HoverAsync();
  await menu.GetByText("Sign out").ClickAsync();
  ```

---

## 5  Selector Recipes for Blazor Components

Blazor generates predictable DOM IDs; but _testability_ improves if you:

```razor
<button data-test="add-widget">Add widget</button>
```

Then in Playwright:

```csharp
await page.GetByTestId("add-widget").ClickAsync();
```

`GetByTestId` is built into Playwright selectors (maps to `[data-testid="xxx"]` and `[data-test="xxx"]`).

---

## 6  Dealing with `#blazor-error-ui`

Blazor shows an overlay when unhandled exceptions occur.  
Add a universal test assertion to fail fast:

```csharp
var errorUi = page.Locator("#blazor-error-ui");
if (await errorUi.IsVisibleAsync())
    Assert.Fail(await errorUi.InnerTextAsync());
```

---

## 7  Running Playwright against HTTPS Dev-certs

* Export the `localhost` dev-cert (`dotnet dev-certs https --export-path cert.pfx -p pass`).
* Tell Playwright to trust it:

```csharp
Chromium.LaunchAsync(new()
{
    Args = new[] { "--ignore-certificate-errors" },
    Headless = true
});
```

For stricter setups, inject the certificate into the browser context instead.

---

## 8  Parallelization Tips

Blazor (Server) uses **SignalR** websockets; multiple simultaneous Playwright pages that hit the same server can saturate connections.  
Set the following to minimize flaky tests:

```csharp
[Collection("Blazor dashboard")]
public class MyTests { /* … */ }

[assembly: CollectionBehavior(MaxParallelThreads = 1)]
```

--- 