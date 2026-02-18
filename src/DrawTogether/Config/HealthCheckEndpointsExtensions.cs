using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DrawTogether.Config;

/// <summary>
/// Extension methods for configuring health check endpoints with tag-based filtering.
/// </summary>
public static class HealthCheckEndpointsExtensions
{
    /// <summary>
    /// Maps three health check endpoints with appropriate filtering:
    /// - /healthz: All health checks (no filtering)
    /// - /healthz/live: Only liveness health checks (self check with liveness tag)
    /// - /healthz/ready: Only readiness health checks (Akka.Persistence checks)
    /// </summary>
    public static IEndpointRouteBuilder MapHealthCheckEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // /healthz - all checks (no filtering)
        endpoints.MapHealthChecks("/healthz", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = WriteHealthCheckResponse
        });

        // /healthz/live - only liveness checks (self check with liveness tag)
        endpoints.MapHealthChecks("/healthz/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("liveness"),
            ResponseWriter = WriteHealthCheckResponse
        });

        // /healthz/ready - readiness checks (Akka.Persistence + cluster membership)
        // Matches: Akka.Persistence health checks (by name), akka.cluster.join (K8s path, "ready" tag),
        // and akka-cluster-membership (Aspire plugin, "akka" tag only — exclude liveness)
        endpoints.MapHealthChecks("/healthz/ready", new HealthCheckOptions
        {
            Predicate = check => check.Name.Contains("Akka.Persistence") ||
                                 check.Tags.Contains("ready") ||
                                 check.Name.Contains("akka-cluster-membership"),
            ResponseWriter = WriteHealthCheckResponse
        });

        return endpoints;
    }

    /// <summary>
    /// Writes a detailed JSON response for health check results.
    /// </summary>
    private static async Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration,
                description = e.Value.Description,
                tags = e.Value.Tags,
                data = e.Value.Data
            })
        };

        await context.Response.WriteAsJsonAsync(payload, new JsonSerializerOptions { WriteIndented = true });
    }
}
