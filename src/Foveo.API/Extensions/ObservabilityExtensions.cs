using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Foveo.API.Extensions;

public static class ObservabilityExtensions
{
    public static void AddObservability(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(r => r
                .AddService(
                    serviceName: "foveo.api.svc",
                    serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString(),
                    serviceInstanceId: Environment.MachineName))
            .WithTracing(b => b
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter())
            .WithMetrics(b => b
                .AddRuntimeInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter());
    }

    public static void AddLogging(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();

        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeScopes = true;
            options.ParseStateValues = true;
            options.AddOtlpExporter();
        });

        builder.Logging.AddSimpleConsole(o =>
        {
            o.SingleLine = true;
            o.TimestampFormat = "HH:mm:ss ";
            o.IncludeScopes = true;
        });
    }
}


