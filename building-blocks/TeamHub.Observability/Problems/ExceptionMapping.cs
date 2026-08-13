namespace TeamHub.Observability.Middleware;

public readonly record struct ExceptionMapping(
    int StatusCode,
    string Title,
    string Detail,
    string Type,
    bool PreferMappedDetail = false,
    IReadOnlyDictionary<string, object?>? Extensions = null);
