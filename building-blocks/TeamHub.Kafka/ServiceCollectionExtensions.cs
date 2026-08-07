using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TeamHub.Kafka;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTeamHubKafkaOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<KafkaOptions>()
            .Bind(configuration.GetSection(KafkaOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    public static IServiceCollection AddTeamHubKafkaProducer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTeamHubKafkaOptions(configuration);
        services.AddSingleton<IKafkaProducer, KafkaProducer>();
        return services;
    }

    public static IServiceCollection AddTeamHubKafkaConsumer<TMessage, THandler>(
        this IServiceCollection services,
        IConfiguration configuration,
        string topic,
        string groupId)
        where THandler : class, IKafkaMessageHandler<TMessage>
    {
        services.AddTeamHubKafkaOptions(configuration);
        services.AddScoped<THandler>();
        services.AddSingleton(new KafkaConsumerRegistration(topic, groupId));
        services.AddHostedService<KafkaConsumerBackgroundService<TMessage, THandler>>();
        return services;
    }
}
