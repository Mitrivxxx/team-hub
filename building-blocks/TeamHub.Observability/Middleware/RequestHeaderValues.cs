using Microsoft.AspNetCore.Http;

namespace TeamHub.Observability.Middleware;

internal static class RequestHeaderValues
{
    public static string? First(IHeaderDictionary headers, string name) =>
        First(headers[name].FirstOrDefault());

    public static string? First(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var token = raw.Split(',')[0].Trim();
        return string.IsNullOrWhiteSpace(token) ? null : token;
    }
}
