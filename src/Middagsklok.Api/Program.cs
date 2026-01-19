using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
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
        policy.WithOrigins("http://localhost:3000")
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

// DateOnly JSON converter
public class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    private const string Format = "yyyy-MM-dd";

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DateOnly.ParseExact(reader.GetString()!, Format);
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format));
    }
}
