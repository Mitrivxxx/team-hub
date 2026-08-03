using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace TeamHub.Observability.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemKey = "CorrelationId";

    static readonly string EmptyTraceId = default(ActivityTraceId).ToString();

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);

        context.Request.Headers[HeaderName] = correlationId;
        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty(ItemKey, correlationId))
        {
            await next(context);
        }
    }

    static string ResolveCorrelationId(HttpContext context)
    {
        var traceId = Activity.Current?.TraceId.ToString();
        if (!string.IsNullOrWhiteSpace(traceId) &&
            !string.Equals(traceId, EmptyTraceId, StringComparison.Ordinal))
        {
            return traceId;
        }

        var incomingHeaderValue = context.Request.Headers[HeaderName].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(incomingHeaderValue))
        {
            return incomingHeaderValue.Trim();
        }

        return Guid.NewGuid().ToString("N");
    }
}
