using Microsoft.EntityFrameworkCore;
using Middagsklok.Database;
using Middagsklok.Database.Repositories;
using Middagsklok.Features;
using Middagsklok.Features.BatchImportDishes;
using Middagsklok.Features.GetDishes;
using Middagsklok.Features.WeeklyPlanning;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddOpenApi();

// Configure database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=middagsklok.db";
builder.Services.AddDbContext<MiddagsklokDbContext>(options =>
    options.UseSqlite(connectionString));

// Register clock
builder.Services.AddSingleton<IClock, SystemClock>();

// Register repositories
builder.Services.AddScoped<IDishRepository, DishRepository>();
builder.Services.AddScoped<IDishImportRepository, DishImportRepository>();
builder.Services.AddScoped<IDishHistoryRepository, DishHistoryRepository>();
builder.Services.AddScoped<IWeeklyPlanRepository, WeeklyPlanRepository>();

// Register features
builder.Services.AddScoped<GetDishesFeature>();
builder.Services.AddScoped<BatchImportDishesFeature>();
builder.Services.AddScoped<GenerateWeeklyPlanFeature>();
builder.Services.AddScoped<GetWeeklyPlanFeature>();
builder.Services.AddScoped<WeeklyPlanRulesValidator>();

// Add database bootstrapper
builder.Services.AddScoped<DbBootstrapper>();

// Add CORS for local development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocal", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowLocal");

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var bootstrapper = scope.ServiceProvider.GetRequiredService<DbBootstrapper>();
    await bootstrapper.InitializeAsync();
}

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck");

// Get all dishes
app.MapGet("/dishes", async (GetDishesFeature feature, CancellationToken ct) =>
{
    var dishes = await feature.Execute(ct);
    return Results.Ok(dishes);
})
.WithName("GetDishes");

// Import dishes
app.MapPost("/dishes/import", async (BatchImportDishesCommand command, BatchImportDishesFeature feature, CancellationToken ct) =>
{
    try
    {
        var result = await feature.Execute(command, ct);
        return Results.Ok(result);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("ImportDishes");

// Generate weekly plan
app.MapPost("/weekly-plans/generate", async (GenerateWeeklyPlanRequest request, GenerateWeeklyPlanFeature feature, CancellationToken ct) =>
{
    try
    {
        var result = await feature.Execute(request, ct);
        return Results.Ok(result);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("GenerateWeeklyPlan");

// Get weekly plan
app.MapGet("/weekly-plans", async (string? weekStart, GetWeeklyPlanFeature feature, CancellationToken ct) =>
{
    DateOnly date;
    if (!string.IsNullOrEmpty(weekStart))
    {
        if (!DateOnly.TryParse(weekStart, out date))
        {
            return Results.BadRequest(new { error = "Invalid date format. Use YYYY-MM-DD." });
        }
    }
    else
    {
        date = DateOnly.FromDateTime(DateTime.Today);
    }
    
    var plan = await feature.Execute(date, ct);
    if (plan == null)
    {
        return Results.NotFound(new { error = "No plan found for the specified week." });
    }
    
    return Results.Ok(plan);
})
.WithName("GetWeeklyPlan");

app.Run();
