namespace TeamHub.Observability;

/// <summary>RFC 9457 problem type URIs for Team Hub APIs.</summary>
public static class ProblemTypes
{
    public const string Base = "https://teamhub.dev/problems";

    public static string Internal { get; } = For("internal");
    public static string ValidationFailed { get; } = For("validation-failed");
    public static string Unauthorized { get; } = For("unauthorized");
    public static string Forbidden { get; } = For("forbidden");
    public static string Conflict { get; } = For("conflict");
    public static string ServiceUnavailable { get; } = For("service-unavailable");

    public static string For(string suffix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suffix);
        return $"{Base}/{suffix.Trim().Trim('/')}";
    }
}
