using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.StackExchangeRedis;
using StackExchange.Redis;

namespace TeamHub.Redis;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTeamHubRedis(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<RedisOptions>()
            .Bind(configuration.GetSection(RedisOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var redisOptions = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
            var connection = ConnectionMultiplexer.Connect(redisOptions.ConnectionString);
            sp.GetService<StackExchangeRedisInstrumentation>()?.AddConnection(connection);
            return connection;
        });
        services.AddSingleton<IRedisKeySegmenter, RedisKeySegmenter>();

        return services;
    }
}
