# HealthChecks Instrumentation for OpenTelemetry .NET

[![Nuget (with prereleases)](https://img.shields.io/nuget/v/HealthChecks.OpenTelemetry.Instrumentation)](https://www.nuget.org/packages/HealthChecks.OpenTelemetry.Instrumentation)
[![NuGet download count badge](https://img.shields.io/nuget/dt/HealthChecks.OpenTelemetry.Instrumentation)](https://www.nuget.org/packages/HealthChecks.OpenTelemetry.Instrumentation)
[![feedz.io](https://img.shields.io/badge/endpoint.svg?url=https%3A%2F%2Ff.feedz.io%2Fgowon%2Fpre-release%2Fshield%2FHealthChecks.OpenTelemetry.Instrumentation%2Flatest)](https://f.feedz.io/gowon/pre-release/packages/HealthChecks.OpenTelemetry.Instrumentation/latest/download)

This is an [Instrumentation Library](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/glossary.md#instrumentation-library), which instruments [Microsoft.Extensions.Diagnostics.HealthChecks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks) and collect telemetry about the application health checks.

## Steps to enable HealthChecks.OpenTelemetry.Instrumentation

### Step 1: Install package

Add a reference to the [`HealthChecks.OpenTelemetry.Instrumentation`](https://www.nuget.org/packages/HealthChecks.OpenTelemetry.Instrumentation) package.

```shell
dotnet add package HealthChecks.OpenTelemetry.Instrumentation
```

### Step 2: Enable HealthChecks Instrumentation

HealthChecks instrumentation should be enabled at application startup using the `AddHealthChecksInstrumentation` extension on `MeterProviderBuilder`. The following example demonstrates adding HealthChecks instrumentation to a console application. This example also sets up the OpenTelemetry Console exporter, which requires adding the package [`OpenTelemetry.Exporter.Console`](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Console/README.md) to the application:

```csharp
using OpenTelemetry;
using OpenTelemetry.Metrics;

public class Program
{
    public static void Main(string[] args)
    {
        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddHealthChecksInstrumentation()
            .AddConsoleExporter()
            .Build();
    }
}
```

For an ASP.NET Core application, adding instrumentation is typically done in the `ConfigureServices` of your `Startup` class. Refer to documentation for [OpenTelemetry.Instrumentation.AspNetCore](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Instrumentation.AspNetCore/README.md).

Refer to [Program.cs](samples/SampleApi/Program.cs) for a complete demo.

### Advanced configuration

This instrumentation can be configured to change the default behavior by using `HealthChecksInstrumentationOptions`.

```csharp
services.AddOpenTelemetry()
    .WithMetrics(builder => builder
        .AddHealthChecksInstrumentation(options =>
        {
            options.StatusGaugeName = "myapp_health";
            options.DurationGaugeName = "myapp_health_duration";
        })
        .AddConsoleExporter());
```

When used with [`OpenTelemetry.Extensions.Hosting`](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Extensions.Hosting/README.md), all configurations to `HealthChecksInstrumentationOptions` can be done in the `ConfigureServices` method of you applications `Startup` class as shown below.

```csharp
// Configure
services.Configure<HealthChecksInstrumentationOptions>(options =>
{
    options.StatusGaugeName = "myapp_health";
    options.DurationGaugeName = "myapp_health_duration";
});

services.AddOpenTelemetry()
    .WithMetrics(builder => builder
        .AddHealthChecksInstrumentation()
        .AddConsoleExporter());
```

## Metrics

### aspnetcore_healthcheck

Gets the health status of the component that was checked, converted to double value (0 == Unhealthy, 0.5 == Degraded, 1 == Healthy).

| Units | Instrument Type | Value Type | Attribute Key(s) | Attribute Values |
|-|-|-|-|-|
| `status` | ObservableGauge | `Double`    | name       | name of each executed health check |

The API used to retrieve the value is:

- [HealthReportEntry.Status](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.diagnostics.healthchecks.healthreportentry.status): Gets the health status of the component that was checked.

### aspnetcore_healthcheck_duration

Gets the health check execution duration.

| Units | Instrument Type | Value Type | Attribute Key(s) | Attribute Values |
|-|-|-|-|-|
| `seconds` | ObservableGauge | `Double`    | name       | name of each executed health check |

The API used to retrieve the value is:

- [HealthReportEntry.Duration](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.diagnostics.healthchecks.healthreportentry.duration): Gets the health check execution duration.

## References

- [OpenTelemetry Project](https://opentelemetry.io/)