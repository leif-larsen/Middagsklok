using Microsoft.EntityFrameworkCore;
using Middagsklok.Database.Configurations;
using Middagsklok.Database.Entities;

namespace Middagsklok.Database;

public class MiddagsklokDbContext : DbContext
{
    public DbSet<DishEntity> Dishes => Set<DishEntity>();
    public DbSet<IngredientEntity> Ingredients => Set<IngredientEntity>();
    public DbSet<DishIngredientEntity> DishIngredients => Set<DishIngredientEntity>();

    public MiddagsklokDbContext(DbContextOptions<MiddagsklokDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DishConfiguration());
        modelBuilder.ApplyConfiguration(new IngredientConfiguration());
        modelBuilder.ApplyConfiguration(new DishIngredientConfiguration());
    }
}
