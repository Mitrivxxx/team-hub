using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TeamHub.Observability.Middleware;

namespace TeamHub.Observability;

/// <summary>Builds RFC 9457 ProblemDetails / ValidationProblemDetails for Team Hub APIs.</summary>
public static class TeamHubProblemDetailsFactory
{
    public const string SessionIdItemKey = "SessionId";
    public const string ProblemJsonContentType = "application/problem+json; charset=utf-8";

    public static ProblemDetails Create(
        HttpContext context,
        int statusCode,
        string title,
        string? detail,
        string type,
        IReadOnlyDictionary<string, object?>? extensions = null,
        string? stackTrace = null)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = type,
            Instance = context.Request.Path.HasValue ? context.Request.Path.Value : null
        };

        ApplyCommonExtensions(problem, context, ResolveCorrelationId(context), extensions, stackTrace);
        return problem;
    }

    public static ValidationProblemDetails CreateValidation(
        HttpContext context,
        ModelStateDictionary modelState,
        int statusCode = StatusCodes.Status400BadRequest,
        string title = "Validation failed",
        string? detail = null,
        string? type = null)
    {
        var problem = new ValidationProblemDetails(modelState)
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = type ?? ProblemTypes.ValidationFailed,
            Instance = context.Request.Path.HasValue ? context.Request.Path.Value : null
        };

        ApplyCommonExtensions(problem, context, ResolveCorrelationId(context), extensions: null, stackTrace: null);
        return problem;
    }

    public static ValidationProblemDetails CreateValidation(
        HttpContext context,
        IDictionary<string, string[]> errors,
        int statusCode,
        string title,
        string? detail,
        string type)
    {
        var problem = new ValidationProblemDetails(errors)
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = type,
            Instance = context.Request.Path.HasValue ? context.Request.Path.Value : null
        };

        ApplyCommonExtensions(problem, context, ResolveCorrelationId(context), extensions: null, stackTrace: null);
        return problem;
    }

    public static ObjectResult ObjectResult(ProblemDetails problem) =>
        new(problem)
        {
            StatusCode = problem.Status,
            ContentTypes = { "application/problem+json" }
        };

    public static string ResolveCorrelationId(HttpContext context)
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

    static void ApplyCommonExtensions(
        ProblemDetails problem,
        HttpContext context,
        string correlationId,
        IReadOnlyDictionary<string, object?>? extensions,
        string? stackTrace)
    {
        problem.Extensions["correlationId"] = correlationId;

        if (context.Items.TryGetValue(SessionIdItemKey, out var sessionId) &&
            sessionId is string sessionIdValue &&
            !string.IsNullOrWhiteSpace(sessionIdValue))
        {
            problem.Extensions["sessionId"] = sessionIdValue;
        }

        if (extensions is not null)
        {
            foreach (var (key, value) in extensions)
            {
                problem.Extensions[key] = value;
            }
        }

        if (!string.IsNullOrWhiteSpace(stackTrace))
        {
            problem.Extensions["stackTrace"] = stackTrace;
        }
    }
}
