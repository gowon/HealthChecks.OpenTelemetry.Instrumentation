namespace HealthChecks.OpenTelemetry.Instrumentation.Tests;

using System.Diagnostics.Metrics;
using global::OpenTelemetry;
using global::OpenTelemetry.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public class HealthChecksMetricsTests
{
    private const int MaxTimeToAllowForFlush = 10000;

    [Fact]
    public void HealthChecksMetricsAreCaptured()
    {
        // Arrange
        HealthChecksInstrumentationOptions? options = null;
        List<Metric> exportedItems = new();
        var result = HealthCheckResult.Healthy();


        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .ConfigureServices(services =>
            {
                // ref: https://github.com/dotnet/aspnetcore/blob/2c111c7aa53c01294f284117eb1ee503940aa4b9/src/HealthChecks/HealthChecks/test/DefaultHealthCheckServiceTest.cs#L626
                services
                    .AddLogging()
                    .AddOptions()
                    .AddHealthChecks()
                    .AddAsyncCheck("TestSample", async () =>
                    {
                        await Task.Delay(100);
                        return result;
                    });
            })
            .AddHealthChecksInstrumentation(o => options = o)
            .AddInMemoryExporter(exportedItems)
            .Build()!;

        // Act
        using MeterListener meterListener = new();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == HealthChecksMetrics.MeterInstance.Name)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        meterListener.RecordObservableInstruments();
        meterProvider.ForceFlush(MaxTimeToAllowForFlush);

        // Assert
        Assert.True(exportedItems.Count == 2);

        var statusMetric = exportedItems.FirstOrDefault(i => i.Name == options!.StatusGaugeName);
        Assert.NotNull(statusMetric);
        Assert.Equal(MetricType.DoubleGauge, statusMetric.MetricType);
        Assert.Equal(HealthChecksMetrics.HealthStatusToMetricValue(result.Status), GetValue(statusMetric));

        var durationMetric = exportedItems.FirstOrDefault(i => i.Name == options!.DurationGaugeName);
        Assert.NotNull(durationMetric);
        Assert.Equal(MetricType.DoubleGauge, durationMetric.MetricType);
        Assert.True(GetValue(durationMetric) > 0);
    }

    // ref: https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/85ee94ca4a55099b1ac8a0552bcec15e01e12729/test/OpenTelemetry.Instrumentation.Runtime.Tests/RuntimeMetricsTests.cs#L188
    private static double GetValue(Metric metric)
    {
        double sum = 0;

        foreach (ref readonly var metricPoint in metric.GetMetricPoints())
        {
            sum += metric.MetricType.IsSum()
                ? metricPoint.GetSumDouble()
                : metricPoint.GetGaugeLastValueDouble();
        }

        return sum;
    }
}