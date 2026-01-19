using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Middagsklok.Api;
using Middagsklok.Api.Endpoints;
using Middagsklok.Database;
using Middagsklok.Database.Repositories;
using Middagsklok.Features.Dishes.Import;
using Middagsklok.Features.Dishes.List;
using Middagsklok.Features.WeeklyPlans.Get;
using Middagsklok.Features.WeeklyPlans.Generate;
using Middagsklok.Features.Shared;
using Middagsklok.Features.WeeklyPlans.Create;
using Middagsklok.Features.DishHistory.Log;
using Middagsklok.Features.DishHistory.Get;
using Middagsklok.Features.DishHistory.GetLastEaten;
using Middagsklok.Features.ShoppingList.GenerateForWeek;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
builder.Services.AddScoped<Middagsklok.Features.Dishes.List.IDishRepository, DishRepository>();
builder.Services.AddScoped<Middagsklok.Features.Dishes.Import.IDishImportRepository, DishImportRepository>();
builder.Services.AddScoped<Middagsklok.Features.WeeklyPlans.Get.IWeeklyPlanRepository, WeeklyPlanRepository>();
builder.Services.AddScoped<Middagsklok.Features.WeeklyPlans.Generate.IWeeklyPlanRepository, WeeklyPlanRepository>();
builder.Services.AddScoped<Middagsklok.Features.WeeklyPlans.Generate.IDishRepository, DishRepository>();
builder.Services.AddScoped<Middagsklok.Features.WeeklyPlans.Generate.IDishHistoryRepository, DishHistoryRepository>();

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

// Enable Swagger in all environments
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Middagsklok API v1");
    c.RoutePrefix = "swagger";
});

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
