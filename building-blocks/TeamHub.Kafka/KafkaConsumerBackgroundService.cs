using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TeamHub.Kafka;

public sealed class KafkaConsumerBackgroundService<TMessage, THandler> : BackgroundService
    where THandler : class, IKafkaMessageHandler<TMessage>
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    readonly IServiceScopeFactory _scopeFactory;
    readonly IOptions<KafkaOptions> _options;
    readonly KafkaConsumerRegistration _registration;
    readonly ILogger<KafkaConsumerBackgroundService<TMessage, THandler>> _logger;

    public KafkaConsumerBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        KafkaConsumerRegistration registration,
        ILogger<KafkaConsumerBackgroundService<TMessage, THandler>> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _registration = registration;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.Run(() => ConsumeLoopAsync(stoppingToken), stoppingToken);

    async Task ConsumeLoopAsync(CancellationToken stoppingToken)
    {
        var kafka = _options.Value;
        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = kafka.BootstrapServers,
            ClientId = $"{kafka.ClientId}-consumer",
            GroupId = _registration.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        }).Build();

        consumer.Subscribe(_registration.Topic);
        _logger.LogInformation(
            "Kafka consumer started for topic {Topic} group {GroupId}",
            _registration.Topic,
            _registration.GroupId);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    if (result?.Message?.Value is null)
                        continue;

                    TMessage? message;
                    try
                    {
                        message = JsonSerializer.Deserialize<TMessage>(result.Message.Value, JsonOptions);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Failed to deserialize Kafka message on {Topic}", _registration.Topic);
                        consumer.Commit(result);
                        continue;
                    }

                    if (message is null)
                    {
                        consumer.Commit(result);
                        continue;
                    }

                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var handler = scope.ServiceProvider.GetRequiredService<THandler>();
                    await handler.HandleAsync(message, stoppingToken);
                    consumer.Commit(result);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error on {Topic}", _registration.Topic);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Kafka handler failed on {Topic}; message will be retried", _registration.Topic);
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }
        }
        finally
        {
            consumer.Close();
        }
    }
}

public sealed record KafkaConsumerRegistration(string Topic, string GroupId);
