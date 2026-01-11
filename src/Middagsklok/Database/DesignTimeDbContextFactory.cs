using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Middagsklok.Database;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MiddagsklokDbContext>
{
    public MiddagsklokDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MiddagsklokDbContext>();
        optionsBuilder.UseSqlite("Data Source=middagsklok.db");

        return new MiddagsklokDbContext(optionsBuilder.Options);
    }
}
