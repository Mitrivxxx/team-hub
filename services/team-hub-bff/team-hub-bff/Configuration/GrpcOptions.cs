namespace team_hub_bff.Configuration;

public sealed class GrpcOptions
{
    public const string SectionName = "Grpc";

    public string Auth { get; set; } = "http://localhost:5101";
    public string Organization { get; set; } = "http://localhost:5102";
}
