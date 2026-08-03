using Microsoft.AspNetCore.Builder;
using TeamHub.Observability.Middleware;

namespace TeamHub.Observability;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseTeamHubExceptionHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<ExceptionMiddleware>();

    public static IApplicationBuilder UseTeamHubCorrelationId(this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationIdMiddleware>();

    public static IApplicationBuilder UseTeamHubUserIdLogging(this IApplicationBuilder app) =>
        app.UseMiddleware<UserIdLoggingMiddleware>();
}
