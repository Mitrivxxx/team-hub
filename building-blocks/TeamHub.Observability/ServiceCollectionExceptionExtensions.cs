using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using TeamHub.Observability.Middleware;

namespace TeamHub.Observability;

public static class ServiceCollectionExceptionExtensions
{
    public static IServiceCollection AddTeamHubExceptionMapper<TMapper>(this IServiceCollection services)
        where TMapper : class, IExceptionProblemDetailsMapper
    {
        services.AddSingleton<IExceptionProblemDetailsMapper, TMapper>();
        return services;
    }

    /// <summary>Configures ModelState / FluentValidation errors as RFC 9457 ValidationProblemDetails.</summary>
    public static IServiceCollection AddTeamHubProblemDetails(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var problem = TeamHubProblemDetailsFactory.CreateValidation(
                    context.HttpContext,
                    context.ModelState);
                return TeamHubProblemDetailsFactory.ObjectResult(problem);
            };
        });

        return services;
    }
}
