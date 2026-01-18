using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
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

// Health endpoint
app.MapGet("/health", () => new { status = "ok" })
    .WithName("GetHealth");

// Dish endpoints
app.MapGet("/dishes", async (GetDishesFeature feature, CancellationToken ct) =>
{
    var dishes = await feature.Execute(ct);
    return Results.Ok(dishes.Select(d => new
    {
        id = d.Id,
        name = d.Name,
        activeMinutes = d.ActiveMinutes,
        totalMinutes = d.TotalMinutes
    }));
})
.WithName("GetDishes");

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

// Weekly plan endpoints
app.MapGet("/weekly-plan/{weekStartDate}", async (string weekStartDate, GetWeeklyPlanFeature feature, CancellationToken ct) =>
{
    if (!DateOnly.TryParse(weekStartDate, out var date))
    {
        return Results.BadRequest(new { error = "Invalid date format. Use YYYY-MM-DD." });
    }

    var plan = await feature.Execute(date, ct);
    if (plan == null)
    {
        return Results.NotFound(new { error = "No plan found for the specified week." });
    }

    return Results.Ok(plan);
})
.WithName("GetWeeklyPlan");

app.MapPost("/weekly-plan/generate", async (GenerateWeeklyPlanRequest request, GenerateWeeklyPlanFeature feature, CancellationToken ct) =>
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

app.Run();

// Custom JSON converter for DateOnly
public class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DateOnly.Parse(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-dd"));
    }
}
