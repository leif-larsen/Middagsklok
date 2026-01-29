namespace Middagsklok.Api.Database;

/// <summary>
/// The exception that is thrown when the required connection string is missing for design-time migrations.
/// </summary>
internal sealed class ConnectionStringMissing : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionStringMissing"/> class.
    /// </summary>
    public ConnectionStringMissing(string message)
        : base(message)
    {
    }
}
