using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TeamHub.Observability.Middleware;

public sealed class ExceptionMiddleware(
    RequestDelegate next,
    IHostEnvironment environment,
    ILogger<ExceptionMiddleware> logger,
    IEnumerable<IExceptionProblemDetailsMapper> mappers)
{
    const string GenericErrorDetail = "An unexpected error occurred. Please contact administrator.";
    const string SessionIdItemKey = "SessionId";

    static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                logger.LogError(ex, "Unhandled exception after response started");
                throw;
            }

            var correlationId = ResolveCorrelationId(context);
            logger.LogError(
                ex,
                "Unhandled exception for request {Method} {Path} (CorrelationId: {CorrelationId})",
                context.Request.Method,
                context.Request.Path,
                correlationId);

            var problem = BuildProblemDetails(context, ex, correlationId);
            context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json; charset=utf-8";
            await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
        }
    }

    static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var item) &&
            item is string correlationIdFromItems &&
            !string.IsNullOrWhiteSpace(correlationIdFromItems))
        {
            return correlationIdFromItems;
        }

        var correlationIdFromHeader = context.Request.Headers[CorrelationIdMiddleware.HeaderName].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(correlationIdFromHeader))
        {
            return correlationIdFromHeader.Trim();
        }

        var traceId = Activity.Current?.TraceId.ToString();
        if (!string.IsNullOrWhiteSpace(traceId))
        {
            return traceId;
        }

        return Guid.NewGuid().ToString("N");
    }

    ProblemDetails BuildProblemDetails(HttpContext context, Exception exception, string correlationId)
    {
        var (statusCode, title, detail, preferMappedDetail) = MapException(exception);

        var detailToReturn = preferMappedDetail
            ? detail
            : environment.IsDevelopment()
                ? exception.Message
                : detail;

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detailToReturn,
            Type = $"https://httpstatuses.com/{statusCode}",
            Instance = context.Request.Path
        };

        problem.Extensions["correlationId"] = correlationId;

        if (context.Items.TryGetValue(SessionIdItemKey, out var sessionId) &&
            sessionId is string sessionIdValue &&
            !string.IsNullOrWhiteSpace(sessionIdValue))
        {
            problem.Extensions["sessionId"] = sessionIdValue;
        }

        if (environment.IsDevelopment())
        {
            problem.Extensions["stackTrace"] = exception.ToString();
        }

        return problem;
    }

    (int StatusCode, string Title, string Detail, bool PreferMappedDetail) MapException(Exception exception)
    {
        foreach (var mapper in mappers)
        {
            if (mapper.TryMap(exception, out var mapping))
            {
                return (mapping.StatusCode, mapping.Title, mapping.Detail, mapping.PreferMappedDetail);
            }
        }

        return (
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred",
            GenericErrorDetail,
            PreferMappedDetail: false);
    }
}
