using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace TeamHub.Observability.Middleware;

public sealed class SessionIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Session-ID";
    public const string ItemKey = "SessionId";

    public async Task InvokeAsync(HttpContext context)
    {
        var sessionId = RequestHeaderValues.First(context.Request.Headers, HeaderName)
            ?? Guid.NewGuid().ToString();

        context.Request.Headers[HeaderName] = sessionId;
        context.Items[ItemKey] = sessionId;
        WriteSingleResponseHeader(context, sessionId);
        context.Response.OnStarting(() =>
        {
            WriteSingleResponseHeader(context, sessionId);
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty(ItemKey, sessionId))
        {
            await next(context);
        }

        if (!context.Response.HasStarted)
        {
            WriteSingleResponseHeader(context, sessionId);
        }
    }

    static void WriteSingleResponseHeader(HttpContext context, string sessionId)
    {
        context.Response.Headers.Remove(HeaderName);
        context.Response.Headers[HeaderName] = sessionId;
    }
}
