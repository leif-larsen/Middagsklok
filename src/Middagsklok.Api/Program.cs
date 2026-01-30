using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Features.Dishes.Import;
using Middagsklok.Api.Features.Dishes.Overview;
using Middagsklok.Api.Features.Ingredients.Create;
using Middagsklok.Api.Features.Ingredients.Metadata;
using Middagsklok.Api.Features.Ingredients.Overview;
using Middagsklok.Api.Features.Ingredients.Update;
using DishesImportUseCase = Middagsklok.Api.Features.Dishes.Import.UseCase;
using DishesOverviewUseCase = Middagsklok.Api.Features.Dishes.Overview.UseCase;
using IngredientsCreateUseCase = Middagsklok.Api.Features.Ingredients.Create.UseCase;
using IngredientsMetadataUseCase = Middagsklok.Api.Features.Ingredients.Metadata.UseCase;
using IngredientsOverviewUseCase = Middagsklok.Api.Features.Ingredients.Overview.UseCase;
using IngredientsUpdateUseCase = Middagsklok.Api.Features.Ingredients.Update.UseCase;

var builder = WebApplication.CreateBuilder(args);

builder.AddNpgsqlDbContext<AppDbContext>("middagsklok");

builder.Services.AddOpenApi();
builder.Services.AddScoped<DishesImportUseCase>();
builder.Services.AddScoped<DishesOverviewUseCase>();
builder.Services.AddScoped<IngredientsCreateUseCase>();
builder.Services.AddScoped<IngredientsMetadataUseCase>();
builder.Services.AddScoped<IngredientsOverviewUseCase>();
builder.Services.AddScoped<IngredientsUpdateUseCase>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevFrontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? Array.Empty<string>();

        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await dbContext.Database.MigrateAsync();

    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("DevFrontend");

DishesImportEndpoint.Map(app);
DishesOverviewEndpoint.Map(app);
IngredientsCreateEndpoint.Map(app);
IngredientsMetadataEndpoint.Map(app);
IngredientsOverviewEndpoint.Map(app);
IngredientsUpdateEndpoint.Map(app);

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
