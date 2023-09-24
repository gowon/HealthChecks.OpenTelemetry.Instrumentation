namespace HealthChecks.OpenTelemetry.Instrumentation;

/// <summary>
/// Options for <see cref="HealthChecksMetrics"/>.
/// </summary>
public class HealthChecksInstrumentationOptions
{
    internal static readonly string HealthCheckDescription =
        "ASP.NET Core health check status (0 == Unhealthy, 0.5 == Degraded, 1 == Healthy)";

    internal static readonly string HealthCheckDurationDescription =
        "Shows duration of the health check execution in seconds";

    public string StatusGaugeName { get; set; } = "aspnetcore_healthcheck";
    public string DurationGaugeName { get; set; } = "aspnetcore_healthcheck_duration";
}