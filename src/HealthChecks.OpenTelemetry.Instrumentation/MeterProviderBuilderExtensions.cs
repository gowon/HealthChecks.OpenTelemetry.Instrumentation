namespace HealthChecks.OpenTelemetry.Instrumentation;

using global::OpenTelemetry.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

/// <summary>
/// Extension methods to simplify registering of dependency instrumentation.
/// </summary>
public static class MeterProviderBuilderExtensions
{
    /// <summary>
    ///     Enables Microsoft.Extensions.Diagnostics.HealthChecks instrumentation.
    /// </summary>
    /// <param name="builder"><see cref="MeterProviderBuilder" /> being configured.</param>
    /// <param name="configure">HealthChecks metrics options.</param>
    /// <returns>The instance of <see cref="MeterProviderBuilder" /> to chain the calls.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static MeterProviderBuilder AddHealthChecksInstrumentation(this MeterProviderBuilder builder,
        Action<HealthChecksInstrumentationOptions>? configure = null)
    {
        _ = builder ?? throw new ArgumentNullException(nameof(builder));

        if (configure != null)
        {
            builder.ConfigureServices(services => services.Configure(configure));
        }

        builder.AddMeter(HealthChecksMetrics.MeterInstance.Name);
        return builder.AddInstrumentation(provider =>
        {
            var healthCheckService = provider.GetRequiredService<HealthCheckService>();
            var options = provider.GetRequiredService<IOptions<HealthChecksInstrumentationOptions>>().Value;
            return new HealthChecksMetrics(healthCheckService, options);
        });
    }
}