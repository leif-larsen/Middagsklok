using Microsoft.EntityFrameworkCore;
using Middagsklok.Database.Configurations;
using Middagsklok.Database.Entities;

namespace Middagsklok.Database;

public class MiddagsklokDbContext : DbContext
{
    public DbSet<DishEntity> Dishes => Set<DishEntity>();
    public DbSet<IngredientEntity> Ingredients => Set<IngredientEntity>();
    public DbSet<DishIngredientEntity> DishIngredients => Set<DishIngredientEntity>();
    public DbSet<WeeklyPlanEntity> WeeklyPlans => Set<WeeklyPlanEntity>();
    public DbSet<WeeklyPlanItemEntity> WeeklyPlanItems => Set<WeeklyPlanItemEntity>();
    public DbSet<DishHistoryEntity> DishHistory => Set<DishHistoryEntity>();

    public MiddagsklokDbContext(DbContextOptions<MiddagsklokDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DishConfiguration());
        modelBuilder.ApplyConfiguration(new IngredientConfiguration());
        modelBuilder.ApplyConfiguration(new DishIngredientConfiguration());
        modelBuilder.ApplyConfiguration(new WeeklyPlanConfiguration());
        modelBuilder.ApplyConfiguration(new WeeklyPlanItemConfiguration());
        modelBuilder.ApplyConfiguration(new DishHistoryConfiguration());
    }
}
