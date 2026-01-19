using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Middagsklok.Api;
using Middagsklok.Api.Endpoints;
using Middagsklok.Database;
using Middagsklok.Database.Repositories;
using Middagsklok.Features;
using Middagsklok.Features.BatchImportDishes;
using Middagsklok.Features.GetDishes;
using Middagsklok.Features.WeeklyPlanning;

var builder = WebApplication.CreateBuilder(args);

// Configure JSON options
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.Converters.Add(new DateOnlyJsonConverter());
});

// Database
builder.Services.AddDbContext<MiddagsklokDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IDishRepository, DishRepository>();
builder.Services.AddScoped<IDishImportRepository, DishImportRepository>();
builder.Services.AddScoped<IWeeklyPlanRepository, WeeklyPlanRepository>();
builder.Services.AddScoped<IDishHistoryRepository, DishHistoryRepository>();

// Features
builder.Services.AddScoped<GetDishesFeature>();
builder.Services.AddScoped<BatchImportDishesFeature>();
builder.Services.AddScoped<GetWeeklyPlanFeature>();
builder.Services.AddScoped<GenerateWeeklyPlanFeature>();
builder.Services.AddScoped<WeeklyPlanRulesValidator>();

// Clock
builder.Services.AddSingleton<IClock, SystemClock>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        // Get allowed origins from configuration (supports Aspire service discovery)
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:3000" };
        
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Ensure database is created (without seed data)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MiddagsklokDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseCors();

// Register endpoints
app.MapHealthEndpoints();
app.MapDishEndpoints();
app.MapWeeklyPlanEndpoints();

app.Run();
