using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Converters;
using Middagsklok.Api.Endpoints;
using Middagsklok.Database;
using Middagsklok.Database.Repositories;
using Middagsklok.Features.BatchImportDishes;
using Middagsklok.Features.GetDishes;
using Middagsklok.Features.WeeklyPlanning;

var builder = WebApplication.CreateBuilder(args);

// Configure JSON serialization
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.Converters.Add(new DateOnlyJsonConverter());
});

// Configure CORS for local frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Configure database
var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Middagsklok", "middagsklok.db");
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

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
builder.Services.AddScoped<GenerateWeeklyPlanFeature>();
builder.Services.AddScoped<GetWeeklyPlanFeature>();
builder.Services.AddScoped<WeeklyPlanRulesValidator>();

// Register services
builder.Services.AddScoped<DbBootstrapper>();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var bootstrapper = scope.ServiceProvider.GetRequiredService<DbBootstrapper>();
    await bootstrapper.InitializeAsync();
}

// Configure middleware
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Register endpoints
app.MapHealthEndpoints();
app.MapDishEndpoints();
app.MapWeeklyPlanEndpoints();

app.Run();
