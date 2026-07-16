namespace TeamHub.Observability;

public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

    public string ServiceName { get; set; } = string.Empty;

    public string OtlpEndpoint { get; set; } = "http://localhost:4317";
}
