using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;
using TeamHub.Observability.Middleware;

namespace TeamHub.Observability;

public static class SerilogRequestLoggingExtensions
{
    public static IApplicationBuilder UseSerilogRequestLoggingExcludingHealth(this IApplicationBuilder app) =>
        app.UseSerilogRequestLogging(options =>
        {
            options.GetLevel = (context, _, exception) =>
                exception is not null
                    ? LogEventLevel.Error
                    : context.Request.Path.StartsWithSegments("/health")
                        || context.Request.Path.StartsWithSegments("/metrics")
                        ? LogEventLevel.Verbose
                        : LogEventLevel.Information;

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                if (httpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var correlationId) &&
                    correlationId is not null)
                {
                    diagnosticContext.Set(CorrelationIdMiddleware.ItemKey, correlationId);
                }

                if (httpContext.Items.TryGetValue(UserIdLoggingMiddleware.ItemKey, out var userId) &&
                    userId is not null)
                {
                    diagnosticContext.Set(UserIdLoggingMiddleware.ItemKey, userId);
                }

                if (httpContext.Items.TryGetValue("SessionId", out var sessionId) &&
                    sessionId is not null)
                {
                    diagnosticContext.Set("SessionId", sessionId);
                }
            };
        });
}
