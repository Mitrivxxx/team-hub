namespace TeamHub.Kafka;

public interface IKafkaProducer
{
    Task ProduceAsync<T>(
        string topic,
        T message,
        string? key = null,
        CancellationToken cancellationToken = default);
}
