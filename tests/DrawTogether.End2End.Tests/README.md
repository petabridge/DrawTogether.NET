# DrawTogether End-to-End Tests

This project contains end-to-end tests for the DrawTogether application using:
- .NET Aspire for hosting the application components
- Microsoft Playwright for UI interactions including mouse and touch events
- xUnit for the test framework

## Prerequisites

1. Install the Playwright browser binaries:

```
pwsh bin/Debug/net8.0/playwright.ps1 install
```

Or using bash:

```
playwright install
```

2. Make sure the test user exists in the authentication system or seed it before tests run.

## Test Structure

- `DrawTogetherFixture.cs`: Collection fixture that starts the application using Aspire
- `DrawingInteractionTests.cs`: Tests for mouse and touch interactions with the canvas
- `AuthenticationTests.cs`: Tests for different authentication scenarios

## Running Tests

Run the tests using the standard .NET test commands:

```
dotnet test
```

## Authentication Methods

The tests demonstrate two authentication approaches:

1. Cookie-based authentication using form login
2. Token-based authentication using JWT bearer tokens

Modify the test credentials in `AuthenticationTests.cs` to match your authentication setup. 