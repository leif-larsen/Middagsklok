using Microsoft.EntityFrameworkCore.Migrations;
using Middagsklok.Api.Database;
using TUnit.Assertions;
using TUnit.Core;

namespace Middagsklok.Tests.Database;

public sealed class MigrationRegistrationTests
{
    // Verifies that every migration class is annotated, since EF only discovers annotated ones
    // and silently reports an out-of-date database as up to date when the attribute is missing.
    [Test]
    public async Task EveryMigrationCarriesTheMigrationAttribute()
    {
        var migrationTypes = typeof(AppDbContext).Assembly
            .GetTypes()
            .Where(type => type.IsSubclassOf(typeof(Migration)) && !type.IsAbstract)
            .ToArray();

        await Assert.That(migrationTypes.Length).IsGreaterThan(0);

        var unregistered = migrationTypes
            .Where(type => type.GetCustomAttributes(typeof(MigrationAttribute), false).Length == 0)
            .Select(type => type.Name)
            .OrderBy(name => name)
            .ToArray();

        await Assert.That(string.Join(", ", unregistered)).IsEqualTo(string.Empty);
    }

    // Verifies that each migration's declared id matches its file-name prefix convention.
    [Test]
    public async Task EveryMigrationIdMatchesItsTypeName()
    {
        var migrationTypes = typeof(AppDbContext).Assembly
            .GetTypes()
            .Where(type => type.IsSubclassOf(typeof(Migration)) && !type.IsAbstract)
            .ToArray();

        var mismatched = new List<string>();

        foreach (var type in migrationTypes)
        {
            var attribute = type
                .GetCustomAttributes(typeof(MigrationAttribute), false)
                .Cast<MigrationAttribute>()
                .FirstOrDefault();

            if (attribute is null || attribute.Id.EndsWith($"_{type.Name}", StringComparison.Ordinal))
            {
                continue;
            }

            mismatched.Add($"{type.Name} declares '{attribute.Id}'");
        }

        await Assert.That(string.Join(", ", mismatched)).IsEqualTo(string.Empty);
    }
}
