namespace HealthChecks.OpenTelemetry.Instrumentation;

using System;
using global::OpenTelemetry.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public static class MeterProviderBuilderExtensions
{
    public static MeterProviderBuilder AddHealthChecksInstrumentation(this MeterProviderBuilder builder,
        Action<HealthChecksInstrumentationOptions>? configureAction = null)
    {
        var options = new HealthChecksInstrumentationOptions();
        configureAction?.Invoke(options);

        builder.AddMeter(HealthChecksMetrics.MeterInstance.Name);
        return builder.AddInstrumentation(provider =>
            new HealthChecksMetrics(provider.GetRequiredService<HealthCheckService>(), options));
    }
}