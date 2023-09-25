namespace SampleApi;

using System.Security.Cryptography;
using HealthChecks.ApplicationStatus.DependencyInjection;
using HealthChecks.OpenTelemetry.Instrumentation;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddHealthChecks()
            .AddApplicationStatus()
            .AddAsyncCheck("random", async () =>
            {
                await Task.Delay(RandomNumberGenerator.GetInt32(100, 501));
                var rng = RandomNumberGenerator.GetInt32(100) % 3;

                switch (rng)
                {
                    case 0:
                        return HealthCheckResult.Unhealthy();

                    case 1:
                        return HealthCheckResult.Degraded();

                    default:
                        return HealthCheckResult.Healthy();
                }
            });

        builder.Services.Configure<HealthChecksInstrumentationOptions>(options =>
        {
            options.StatusGaugeName = "myapp_health";
            options.DurationGaugeName = "myapp_health_duration";
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metricsBuilder => metricsBuilder
                .AddHealthChecksInstrumentation()
                .AddPrometheusExporter()
                .AddConsoleExporter());

        var app = builder.Build();
        
        app.MapGet("/", () => "Hello World!");

        app.UseOpenTelemetryPrometheusScrapingEndpoint();

        app.Run();
    }
}