# DrawTogether.NET

## Aspire MCP Integration

This project uses the .NET Aspire MCP server for runtime observability. When the AppHost is running (via `dotnet run` in `src/DrawTogether.AppHost` or `aspire run`), use the `mcp__aspire__*` tools to inspect resources, logs, traces, and health status.

### Aspire Skills

When working with Aspire configuration or debugging Aspire-related issues, invoke these dotnet-skills:

- `dotnet-skills:aspire-configuration` — AppHost-to-app config wiring, explicit env var mapping, feature toggles
- `dotnet-skills:aspire-integration-testing` — Integration tests using Aspire's `DistributedApplicationTestingBuilder`
- `dotnet-skills:aspire-service-defaults` — Shared ServiceDefaults project for OpenTelemetry, health checks, resilience
- `dotnet-skills:akka-aspire-configuration` — Akka.NET + Aspire clustering, persistence, and management setup
