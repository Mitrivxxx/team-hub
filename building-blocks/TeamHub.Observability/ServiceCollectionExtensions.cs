using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace TeamHub.Observability;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTeamHubOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        bool includeEntityFrameworkCore = false)
    {
        var options = configuration.GetSection(ObservabilityOptions.SectionName).Get<ObservabilityOptions>()
            ?? new ObservabilityOptions { ServiceName = serviceName };

        if (string.IsNullOrWhiteSpace(options.ServiceName))
        {
            options.ServiceName = serviceName;
        }

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(options.ServiceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(aspNetCoreOptions =>
                    {
                        aspNetCoreOptions.RecordException = true;
                        aspNetCoreOptions.Filter = context => !IsExcludedPath(context.Request.Path);
                    })
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(otlp => otlp.Endpoint = new Uri(options.OtlpEndpoint));

                if (includeEntityFrameworkCore)
                {
                    tracing.AddEntityFrameworkCoreInstrumentation();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddPrometheusExporter();
            });

        return services;
    }

    static bool IsExcludedPath(PathString path) =>
        path.StartsWithSegments("/health") || path.StartsWithSegments("/metrics");
}
