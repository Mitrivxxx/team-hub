namespace TeamHub.Kafka;

public interface IKafkaMessageHandler<in TMessage>
{
    Task HandleAsync(TMessage message, CancellationToken cancellationToken);
}
