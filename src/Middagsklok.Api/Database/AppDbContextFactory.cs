using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Middagsklok.Api.Database;

internal sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <summary>
    /// Creates a design-time <see cref="AppDbContext"/> for EF Core tooling.
    /// </summary>
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("middagsklok");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ConnectionStringMissing("Connection string 'middagsklok' is required for design-time migrations.");
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
