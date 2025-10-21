using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DrawTogether.End2End.Tests;

[Collection("DrawTogether")]
public class HealthCheckTests
{
    private readonly DrawTogetherFixture _fixture;
    private readonly HttpClient _httpClient;

    public HealthCheckTests(DrawTogetherFixture fixture)
    {
        _fixture = fixture;

        // Create HttpClient with handler that accepts self-signed certificates (for testing only)
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = _fixture.GetDrawTogetherEndpoint()
        };
    }

    [Fact]
    public async Task HealthzEndpoint_ShouldReturnHealthy()
    {
        // Act
        var response = await _httpClient.GetAsync("/healthz");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var healthReport = JsonSerializer.Deserialize<HealthReportResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(healthReport);
        Assert.Equal("Healthy", healthReport.Status);
        Assert.NotEmpty(healthReport.Checks);

        // Should include all checks (liveness + readiness)
        Assert.Contains(healthReport.Checks, c => c.Name == "self");
        Assert.Contains(healthReport.Checks, c => c.Name.Contains("Akka.Persistence.Sql"));
    }

    [Fact]
    public async Task HealthzLiveEndpoint_ShouldReturnHealthy()
    {
        // Act
        var response = await _httpClient.GetAsync("/healthz/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var healthReport = JsonSerializer.Deserialize<HealthReportResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(healthReport);
        Assert.Equal("Healthy", healthReport.Status);
        Assert.NotEmpty(healthReport.Checks);

        // Should only include liveness checks
        Assert.Contains(healthReport.Checks, c => c.Name == "self");
        Assert.All(healthReport.Checks, check =>
        {
            Assert.Contains("liveness", check.Tags);
        });
    }

    [Fact]
    public async Task HealthzReadyEndpoint_ShouldReturnHealthy()
    {
        // Act
        var response = await _httpClient.GetAsync("/healthz/ready");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var healthReport = JsonSerializer.Deserialize<HealthReportResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(healthReport);
        Assert.Equal("Healthy", healthReport.Status);
        Assert.NotEmpty(healthReport.Checks);

        // Should only include readiness checks (Akka.Persistence health checks filtered by name)
        Assert.All(healthReport.Checks, check =>
        {
            Assert.Contains("Akka.Persistence", check.Name);
        });

        // Verify Akka.Persistence checks are present
        Assert.Contains(healthReport.Checks, c => c.Name.Contains("Akka.Persistence.Sql.Journal"));
        Assert.Contains(healthReport.Checks, c => c.Name.Contains("Akka.Persistence.Sql.SnapshotStore"));
    }
}

// Response DTOs to match the health check JSON response format
public record HealthReportResponse(
    string Status,
    TimeSpan TotalDuration,
    List<HealthCheckEntry> Checks
);

public record HealthCheckEntry(
    string Name,
    string Status,
    TimeSpan Duration,
    string? Description,
    List<string> Tags,
    Dictionary<string, object>? Data
);
