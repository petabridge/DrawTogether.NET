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
    public static IServiceCollection AddDrawTogetherOtel(this IServiceCollection services,
        string otelHostName = "localhost")
    {
        var otelEndpoint = new Uri($"http://{otelHostName}:4317");

        services
            .AddOpenTelemetry()
            .ConfigureResource(builder =>
            {
                builder
                    .AddEnvironmentVariableDetector()
                    .AddTelemetrySdk();
            })
            .UseOtlpExporter(OtlpExportProtocol.Grpc, otelEndpoint)
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