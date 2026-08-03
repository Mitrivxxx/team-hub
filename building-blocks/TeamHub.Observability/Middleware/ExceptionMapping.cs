namespace TeamHub.Observability.Middleware;

public readonly record struct ExceptionMapping(
    int StatusCode,
    string Title,
    string Detail,
    bool PreferMappedDetail = false);
