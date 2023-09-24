namespace HealthChecks.OpenTelemetry.Instrumentation;

using System.Diagnostics.Metrics;
using System.Reflection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

internal class HealthChecksMetrics
{
    internal static readonly AssemblyName AssemblyName = typeof(HealthChecksMetrics).Assembly.GetName();

    // ref: https://www.thorsten-hans.com/instrumenting-dotnet-apps-with-opentelemetry
    internal static readonly Meter MeterInstance = new(AssemblyName.Name, AssemblyName.Version.ToString());

    private HealthReport? _sharedReport;
    private bool _useCachedReport;

    public HealthChecksMetrics(HealthCheckService healthCheckService, HealthChecksInstrumentationOptions options)
    {
        _ = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));

        MeterInstance.CreateObservableGauge(options.StatusGaugeName,
            () =>
            {
                if (!_useCachedReport || _sharedReport == null)
                {
                    _useCachedReport = true;
                    _sharedReport = healthCheckService.CheckHealthAsync().GetAwaiter().GetResult();
                    return _sharedReport.Entries.Select(entry => new Measurement<double>(
                        HealthStatusToMetricValue(entry.Value.Status),
                        new KeyValuePair<string, object?>("name", entry.Key)));
                }

                _useCachedReport = false;
                return _sharedReport.Entries.Select(entry => new Measurement<double>(
                    HealthStatusToMetricValue(entry.Value.Status),
                    new KeyValuePair<string, object?>("name", entry.Key)));
            }, "status", HealthChecksInstrumentationOptions.HealthCheckDescription);

        MeterInstance.CreateObservableGauge(options.DurationGaugeName,
            () =>
            {
                if (!_useCachedReport || _sharedReport == null)
                {
                    _useCachedReport = true;
                    _sharedReport = healthCheckService.CheckHealthAsync().GetAwaiter().GetResult();
                    return _sharedReport.Entries.Select(entry => new Measurement<double>(
                        entry.Value.Duration.TotalSeconds,
                        new KeyValuePair<string, object?>("name", entry.Key)));
                }

                _useCachedReport = false;
                return _sharedReport.Entries.Select(entry => new Measurement<double>(entry.Value.Duration.TotalSeconds,
                    new KeyValuePair<string, object?>("name", entry.Key)));
            }, "seconds", HealthChecksInstrumentationOptions.HealthCheckDurationDescription);
    }

    internal static double HealthStatusToMetricValue(HealthStatus status)
    {
        switch (status)
        {
            case HealthStatus.Unhealthy:
                return 0;
            case HealthStatus.Degraded:
                return 0.5;
            case HealthStatus.Healthy:
                return 1;
            default:
                throw new NotSupportedException($"Unexpected HealthStatus value: {status}");
        }
    }
}