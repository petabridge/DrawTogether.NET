// -----------------------------------------------------------------------
//  <copyright file="OpenTelemetryConfig.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2024 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace DrawTogether.Config;

public static class OpenTelemetryConfig
{
    public static IServiceCollection AddDrawTogetherOtel(this IServiceCollection services)
    {
        
        // work around for https://github.com/open-telemetry/opentelemetry-dotnet/issues/5586
        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http:/localhost:4317";

        services
            .AddOpenTelemetry()
            .ConfigureResource(builder =>
            {
                builder
                    .AddEnvironmentVariableDetector()
                    .AddTelemetrySdk()
                    .AddAttributes(new []
                    {
                        new KeyValuePair<string, object>("service.version", typeof(OpenTelemetryConfig).Assembly.GetName().Version?.ToString() ?? "unknown")
                    });
            })
            .UseOtlpExporter(OtlpExportProtocol.Grpc, new Uri(otlpEndpoint))
            .WithMetrics(c =>
            {
                c.AddRuntimeInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation();
            })
            .WithTracing(c =>
            {
                c.AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation();
            });

        return services;
    }
}