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

            var correlationId = TeamHubProblemDetailsFactory.ResolveCorrelationId(context);
            logger.LogError(
                ex,
                "Unhandled exception for request {Method} {Path} (CorrelationId: {CorrelationId})",
                context.Request.Method,
                context.Request.Path,
                correlationId);

            var problem = BuildProblemDetails(context, ex);
            context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
            context.Response.ContentType = TeamHubProblemDetailsFactory.ProblemJsonContentType;
            await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
        }
    }

    ProblemDetails BuildProblemDetails(HttpContext context, Exception exception)
    {
        var mapping = MapException(exception);

        var detailToReturn = mapping.PreferMappedDetail
            ? mapping.Detail
            : environment.IsDevelopment()
                ? exception.Message
                : mapping.Detail;

        var stackTrace = environment.IsDevelopment() ? exception.ToString() : null;

        return TeamHubProblemDetailsFactory.Create(
            context,
            mapping.StatusCode,
            mapping.Title,
            detailToReturn,
            mapping.Type,
            mapping.Extensions,
            stackTrace);
    }

    ExceptionMapping MapException(Exception exception)
    {
        foreach (var mapper in mappers)
        {
            if (mapper.TryMap(exception, out var mapping))
            {
                return mapping;
            }
        }

        return new ExceptionMapping(
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred",
            GenericErrorDetail,
            ProblemTypes.Internal,
            PreferMappedDetail: false);
    }
}
