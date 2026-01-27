namespace Middagsklok.Api.Database;

using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Domain.Dish;
using Middagsklok.Api.Domain.Ingredient;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<Dish> Dishes => Set<Dish>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
