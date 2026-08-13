using Microsoft.AspNetCore.Http;

namespace TeamHub.Observability;

internal static class ObservabilityPaths
{
    public const string Health = "/health";
    public const string Metrics = "/metrics";

    public static bool IsExcluded(PathString path) =>
        path.StartsWithSegments(Health) || path.StartsWithSegments(Metrics);
}
