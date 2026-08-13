using Microsoft.AspNetCore.Builder;
using OpenTelemetry.Metrics;

namespace TeamHub.Observability;

public static class WebApplicationExtensions
{
    public static WebApplication MapTeamHubObservabilityEndpoints(this WebApplication app)
    {
        app.MapPrometheusScrapingEndpoint(ObservabilityPaths.Metrics);
        return app;
    }
}
