using System.Reflection;
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
        bool includeEntityFrameworkCore = false,
        bool includeStackExchangeRedis = false)
    {
        var section = configuration.GetSection(ObservabilityOptions.SectionName);
        var options = section.Get<ObservabilityOptions>()
            ?? new ObservabilityOptions { ServiceName = serviceName };

        if (string.IsNullOrWhiteSpace(options.ServiceName))
        {
            options.ServiceName = serviceName;
        }

        options.OtlpEndpoint = ResolveOtlpEndpoint(section["OtlpEndpoint"]);

        var environmentName = configuration["ASPNETCORE_ENVIRONMENT"]
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Production";
        var serviceVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? "0.0.0";

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(options.ServiceName, serviceVersion: serviceVersion)
                .AddAttributes(
                [
                    new KeyValuePair<string, object>("deployment.environment", environmentName)
                ]))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(ObservabilitySources.Kafka)
                    .AddAspNetCoreInstrumentation(aspNetCoreOptions =>
                    {
                        aspNetCoreOptions.RecordException = true;
                        aspNetCoreOptions.Filter = context => !ObservabilityPaths.IsExcluded(context.Request.Path);
                    })
                    .AddHttpClientInstrumentation()
                    .AddGrpcClientInstrumentation()
                    .AddOtlpExporter(otlp => otlp.Endpoint = new Uri(options.OtlpEndpoint));

                if (includeEntityFrameworkCore)
                {
                    tracing.AddEntityFrameworkCoreInstrumentation();
                }

                if (includeStackExchangeRedis)
                {
                    tracing.AddRedisInstrumentation();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(options.ServiceName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddPrometheusExporter();
            });

        return services;
    }

    static string ResolveOtlpEndpoint(string? configuredEndpoint)
    {
        if (!string.IsNullOrWhiteSpace(configuredEndpoint))
        {
            return configuredEndpoint;
        }

        var fromEnv = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv;
        }

        return "http://localhost:4317";
    }
}
