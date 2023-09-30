namespace HealthChecks.OpenTelemetry.Instrumentation;

using System.Diagnostics.Metrics;
using System.Reflection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

internal class HealthChecksMetrics
{
    internal static readonly AssemblyName AssemblyName = typeof(HealthChecksMetrics).Assembly.GetName();

    // ref: https://www.thorsten-hans.com/instrumenting-dotnet-apps-with-opentelemetry
    internal static readonly Meter MeterInstance = new(AssemblyName.Name, AssemblyName.Version.ToString());

    private readonly HealthCheckService _healthCheckService;
    private HealthReport? _cachedReport;
    private bool _useCachedReport;

    public HealthChecksMetrics(HealthCheckService healthCheckService, HealthChecksInstrumentationOptions options)
    {
        _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));

        MeterInstance.CreateObservableGauge(options.StatusGaugeName,
            () => ProcessCachedReport((name, entry) => new Measurement<double>(HealthStatusToMetricValue(entry.Status),
                new KeyValuePair<string, object?>("name", name))), "status",
            HealthChecksInstrumentationOptions.HealthCheckDescription);

        MeterInstance.CreateObservableGauge(options.DurationGaugeName,
            () => ProcessCachedReport((name, entry) => new Measurement<double>(entry.Duration.TotalSeconds,
                new KeyValuePair<string, object?>("name", name))), "seconds",
            HealthChecksInstrumentationOptions.HealthCheckDurationDescription);
    }

    private IEnumerable<Measurement<double>> ProcessCachedReport(
        Func<string, HealthReportEntry, Measurement<double>> processEntry)
    {
        if (!_useCachedReport || _cachedReport == null)
        {
            _useCachedReport = true;
            _cachedReport = _healthCheckService.CheckHealthAsync().GetAwaiter().GetResult();
            return _cachedReport.Entries.Select(entry => processEntry.Invoke(entry.Key, entry.Value));
        }

        _useCachedReport = false;
        return _cachedReport.Entries.Select(entry => processEntry.Invoke(entry.Key, entry.Value));
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