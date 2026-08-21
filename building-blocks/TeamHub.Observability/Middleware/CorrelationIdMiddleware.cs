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
        WriteSingleResponseHeader(context, correlationId);
        context.Response.OnStarting(() =>
        {
            WriteSingleResponseHeader(context, correlationId);
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty(ItemKey, correlationId))
        {
            await next(context);
        }

        if (!context.Response.HasStarted)
        {
            WriteSingleResponseHeader(context, correlationId);
        }
    }

    static void WriteSingleResponseHeader(HttpContext context, string correlationId)
    {
        context.Response.Headers.Remove(HeaderName);
        context.Response.Headers[HeaderName] = correlationId;
    }

    static string ResolveCorrelationId(HttpContext context)
    {
        var traceId = Activity.Current?.TraceId.ToString();
        if (!string.IsNullOrWhiteSpace(traceId) &&
            !string.Equals(traceId, EmptyTraceId, StringComparison.Ordinal))
        {
            return traceId;
        }

        var incomingHeaderValue = RequestHeaderValues.First(context.Request.Headers, HeaderName);
        if (!string.IsNullOrWhiteSpace(incomingHeaderValue))
        {
            return incomingHeaderValue;
        }

        return Guid.NewGuid().ToString("N");
    }
}
