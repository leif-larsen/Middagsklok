using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Middagsklok.Database;
using Middagsklok.Database.Repositories;
using Middagsklok.Features;
using Middagsklok.Features.BatchImportDishes;
using Middagsklok.Features.GetDishes;
using Middagsklok.Features.WeeklyPlanning;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

// Configure JSON options
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure Database
var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "middagsklok.db");
builder.Services.AddDbContext<MiddagsklokDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Register repositories
builder.Services.AddScoped<IDishRepository, DishRepository>();
builder.Services.AddScoped<IDishImportRepository, DishImportRepository>();
builder.Services.AddScoped<IWeeklyPlanRepository, WeeklyPlanRepository>();
builder.Services.AddScoped<IDishHistoryRepository, DishHistoryRepository>();

// Register features
builder.Services.AddScoped<GetDishesFeature>();
builder.Services.AddScoped<BatchImportDishesFeature>();
builder.Services.AddScoped<GetWeeklyPlanFeature>();
builder.Services.AddScoped<GenerateWeeklyPlanFeature>();

// Register infrastructure services
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<WeeklyPlanRulesValidator>();
builder.Services.AddScoped<DbBootstrapper>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var bootstrapper = scope.ServiceProvider.GetRequiredService<DbBootstrapper>();
    await bootstrapper.InitializeAsync();
}

// Health endpoint
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Dishes endpoints
app.MapGet("/dishes", async (GetDishesFeature feature, CancellationToken ct) =>
{
    var dishes = await feature.Execute(ct);
    var response = dishes.Select(d => new
    {
        id = d.Id,
        name = d.Name,
        activeMinutes = d.ActiveMinutes,
        totalMinutes = d.TotalMinutes,
        kidRating = d.KidRating,
        familyRating = d.FamilyRating,
        isPescetarian = d.IsPescetarian,
        hasOptionalMeatVariant = d.HasOptionalMeatVariant,
        ingredients = d.Ingredients.Select(i => new
        {
            name = i.Ingredient.Name,
            category = i.Ingredient.Category,
            amount = i.Amount,
            unit = i.Unit,
            optional = i.Optional
        }).ToList()
    }).ToList();
    
    return Results.Ok(response);
});

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
});

// Weekly plan endpoints
app.MapGet("/weekly-plan/{weekStartDate}", async (string weekStartDate, GetWeeklyPlanFeature feature, CancellationToken ct) =>
{
    if (!DateOnly.TryParse(weekStartDate, out var date))
    {
        return Results.BadRequest(new { error = "Invalid date format. Use YYYY-MM-DD" });
    }
    
    var plan = await feature.Execute(date, ct);
    if (plan is null)
    {
        return Results.NotFound(new { error = "No weekly plan found for the specified week" });
    }
    
    var response = new
    {
        id = plan.Id,
        weekStartDate = plan.WeekStartDate,
        createdAt = plan.CreatedAt,
        items = plan.Items.Select(item => new
        {
            dayIndex = item.DayIndex,
            dish = new
            {
                id = item.Dish.Id,
                name = item.Dish.Name,
                activeMinutes = item.Dish.ActiveMinutes,
                totalMinutes = item.Dish.TotalMinutes,
                kidRating = item.Dish.KidRating,
                familyRating = item.Dish.FamilyRating,
                isPescetarian = item.Dish.IsPescetarian,
                hasOptionalMeatVariant = item.Dish.HasOptionalMeatVariant
            }
        }).ToList()
    };
    
    return Results.Ok(response);
});

app.MapPost("/weekly-plan/generate", async (GenerateWeeklyPlanRequest request, GenerateWeeklyPlanFeature feature, CancellationToken ct) =>
{
    try
    {
        var result = await feature.Execute(request, ct);
        
        var response = new
        {
            id = result.Plan.Id,
            weekStartDate = result.Plan.WeekStartDate,
            createdAt = result.Plan.CreatedAt,
            items = result.Plan.Items.Select(item => new
            {
                dayIndex = item.DayIndex,
                dish = new
                {
                    id = item.Dish.Id,
                    name = item.Dish.Name,
                    activeMinutes = item.Dish.ActiveMinutes,
                    totalMinutes = item.Dish.TotalMinutes,
                    kidRating = item.Dish.KidRating,
                    familyRating = item.Dish.FamilyRating,
                    isPescetarian = item.Dish.IsPescetarian,
                    hasOptionalMeatVariant = item.Dish.HasOptionalMeatVariant
                },
                explanation = result.ExplanationsByDay.TryGetValue(item.DayIndex, out var exp) 
                    ? new { dishId = exp.DishId, reasons = exp.Reasons }
                    : null
            }).ToList()
        };
        
        return Results.Ok(response);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.Run();
