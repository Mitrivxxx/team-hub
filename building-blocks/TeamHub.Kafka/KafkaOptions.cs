using System.ComponentModel.DataAnnotations;

namespace TeamHub.Kafka;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    [Required]
    public string BootstrapServers { get; init; } = string.Empty;

    public string ClientId { get; init; } = "team-hub";
}
