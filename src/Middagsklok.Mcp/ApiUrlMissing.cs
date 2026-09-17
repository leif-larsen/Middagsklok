namespace Middagsklok.Mcp;

/// <summary>
/// The exception that is thrown when the MIDDAGSKLOK_API_URL environment variable is not set.
/// </summary>
public sealed class ApiUrlMissing(string message) : Exception(message);
