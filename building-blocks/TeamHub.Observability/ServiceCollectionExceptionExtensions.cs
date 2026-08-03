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
}
