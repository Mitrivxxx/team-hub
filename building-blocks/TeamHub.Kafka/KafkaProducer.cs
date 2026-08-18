using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TeamHub.Kafka;

public sealed class KafkaProducer : IKafkaProducer, IDisposable
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    readonly IProducer<string, string> _producer;
    readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(IOptions<KafkaOptions> options, ILogger<KafkaProducer> logger)
    {
        _logger = logger;
        var kafka = options.Value;
        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = kafka.BootstrapServers,
            ClientId = kafka.ClientId,
            Acks = Acks.All,
            EnableIdempotence = true
        }).Build();
    }

    public async Task ProduceAsync<T>(
        string topic,
        T message,
        string? key = null,
        CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(message, JsonOptions);
        await ProduceRawAsync(topic, payload, key, cancellationToken);
    }

    public async Task ProduceRawAsync(
        string topic,
        string payload,
        string? key = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = KafkaActivity.StartProduce(topic);
        try
        {
            var result = await _producer.ProduceAsync(
                topic,
                new Message<string, string>
                {
                    Key = key ?? string.Empty,
                    Value = payload
                },
                cancellationToken);

            activity?.SetTag("messaging.destination.partition.id", result.Partition.Value);
            activity?.SetTag("messaging.kafka.offset", result.Offset.Value);

            _logger.LogInformation(
                "Produced Kafka message to {Topic} partition {Partition} offset {Offset}",
                result.Topic,
                result.Partition.Value,
                result.Offset.Value);
        }
        catch (Exception ex)
        {
            KafkaActivity.SetError(activity, ex);
            throw;
        }
    }

    public void Dispose() => _producer.Dispose();
}
