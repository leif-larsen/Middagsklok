using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Domain.Dish;
using Middagsklok.Api.Domain.Ingredient;
using Middagsklok.Api.Domain.Settings;
using Middagsklok.Api.Domain.WeeklyPlan;

namespace Middagsklok.Api.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<Dish> Dishes => Set<Dish>();
    public DbSet<PlanningSettings> PlanningSettings => Set<PlanningSettings>();
    public DbSet<WeeklyPlan> WeeklyPlans => Set<WeeklyPlan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
