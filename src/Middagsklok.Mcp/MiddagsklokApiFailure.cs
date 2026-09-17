using System.Net;

namespace Middagsklok.Mcp;

/// <summary>
/// The exception that is thrown when the Middagsklok API answers with a status the tool cannot act on.
/// </summary>
public sealed class MiddagsklokApiFailure(HttpStatusCode statusCode, string route, string body)
    : Exception($"Middagsklok API returned {(int)statusCode} for {route}: {body}")
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string Route { get; } = route;
    public string Body { get; } = body;
}
