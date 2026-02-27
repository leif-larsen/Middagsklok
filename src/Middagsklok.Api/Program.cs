using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Features.Dishes.Create;
using Middagsklok.Api.Features.Dishes.Delete;
using Middagsklok.Api.Features.Dishes.Import;
using Middagsklok.Api.Features.Dishes.Lookup;
using Middagsklok.Api.Features.Dishes.Metadata;
using Middagsklok.Api.Features.Dishes.Overview;
using Middagsklok.Api.Features.Dishes.Update;
using Middagsklok.Api.Features.Ingredients.Create;
using Middagsklok.Api.Features.Ingredients.Delete;
using Middagsklok.Api.Features.Ingredients.Metadata;
using Middagsklok.Api.Features.Ingredients.Overview;
using Middagsklok.Api.Features.Ingredients.Update;
using Middagsklok.Api.Features.Recipes.Instructions;
using Middagsklok.Api.Features.Recipes.Suggestions;
using Middagsklok.Api.Features.Settings.Get;
using Middagsklok.Api.Features.Settings.Upsert;
using Middagsklok.Api.Features.ShoppingList.ByStartDate;
using Middagsklok.Api.Features.WeeklyPlans.Available;
using Middagsklok.Api.Features.WeeklyPlans.ByStartDate;
using Middagsklok.Api.Features.WeeklyPlans.Generate;
using Middagsklok.Api.Features.WeeklyPlans.MarkEaten;
using Middagsklok.Api.Features.WeeklyPlans.PlannedDishByDate;
using Middagsklok.Api.Features.WeeklyPlans.Upsert;
using DishesCreateUseCase = Middagsklok.Api.Features.Dishes.Create.UseCase;
using DishesDeleteUseCase = Middagsklok.Api.Features.Dishes.Delete.UseCase;
using DishesImportUseCase = Middagsklok.Api.Features.Dishes.Import.UseCase;
using DishesLookupUseCase = Middagsklok.Api.Features.Dishes.Lookup.UseCase;
using DishesMetadataUseCase = Middagsklok.Api.Features.Dishes.Metadata.UseCase;
using DishesOverviewUseCase = Middagsklok.Api.Features.Dishes.Overview.UseCase;
using DishesUpdateUseCase = Middagsklok.Api.Features.Dishes.Update.UseCase;
using IngredientsCreateUseCase = Middagsklok.Api.Features.Ingredients.Create.UseCase;
using IngredientsDeleteUseCase = Middagsklok.Api.Features.Ingredients.Delete.UseCase;
using IngredientsMetadataUseCase = Middagsklok.Api.Features.Ingredients.Metadata.UseCase;
using IngredientsOverviewUseCase = Middagsklok.Api.Features.Ingredients.Overview.UseCase;
using IngredientsUpdateUseCase = Middagsklok.Api.Features.Ingredients.Update.UseCase;
using PlanningSettingsGetUseCase = Middagsklok.Api.Features.Settings.Get.UseCase;
using PlanningSettingsUpsertUseCase = Middagsklok.Api.Features.Settings.Upsert.UseCase;
using RecipesInstructionsUseCase = Middagsklok.Api.Features.Recipes.Instructions.UseCase;
using RecipesSuggestionsUseCase = Middagsklok.Api.Features.Recipes.Suggestions.UseCase;
using ShoppingListByStartDateUseCase = Middagsklok.Api.Features.ShoppingList.ByStartDate.UseCase;
using WeeklyPlansAvailableUseCase = Middagsklok.Api.Features.WeeklyPlans.Available.UseCase;
using WeeklyPlansByStartDateUseCase = Middagsklok.Api.Features.WeeklyPlans.ByStartDate.UseCase;
using WeeklyPlansGenerateUseCase = Middagsklok.Api.Features.WeeklyPlans.Generate.UseCase;
using WeeklyPlansMarkEatenUseCase = Middagsklok.Api.Features.WeeklyPlans.MarkEaten.UseCase;
using PlannedDishByDateUseCase = Middagsklok.Api.Features.WeeklyPlans.PlannedDishByDate.UseCase;
using WeeklyPlansUpsertUseCase = Middagsklok.Api.Features.WeeklyPlans.Upsert.UseCase;

var builder = WebApplication.CreateBuilder(args);

builder.AddNpgsqlDbContext<AppDbContext>("middagsklok");

builder.Services.AddOpenApi();
builder.Services.AddScoped<DishesCreateUseCase>();
builder.Services.AddScoped<DishesDeleteUseCase>();
builder.Services.AddScoped<DishesImportUseCase>();
builder.Services.AddScoped<DishesLookupUseCase>();
builder.Services.AddScoped<DishesMetadataUseCase>();
builder.Services.AddScoped<DishesOverviewUseCase>();
builder.Services.AddScoped<DishesUpdateUseCase>();
builder.Services.AddScoped<IngredientsCreateUseCase>();
builder.Services.AddScoped<IngredientsDeleteUseCase>();
builder.Services.AddScoped<IngredientsMetadataUseCase>();
builder.Services.AddScoped<IngredientsOverviewUseCase>();
builder.Services.AddScoped<IngredientsUpdateUseCase>();
builder.Services.AddScoped<IRecipeSuggestionClientSelector, RecipeSuggestionClientSelector>();
builder.Services.AddScoped<RecipesInstructionsUseCase>();
builder.Services.AddScoped<RecipesSuggestionsUseCase>();
builder.Services.AddScoped<PlanningSettingsGetUseCase>();
builder.Services.AddScoped<PlanningSettingsUpsertUseCase>();
builder.Services.AddScoped<ShoppingListByStartDateUseCase>();
builder.Services.AddScoped<WeeklyPlansAvailableUseCase>();
builder.Services.AddScoped<WeeklyPlansByStartDateUseCase>();
builder.Services.AddScoped<WeeklyPlansGenerateUseCase>();
builder.Services.AddScoped<WeeklyPlansMarkEatenUseCase>();
builder.Services.AddScoped<PlannedDishByDateUseCase>();
builder.Services.AddScoped<WeeklyPlansUpsertUseCase>();
builder.Services.AddHttpClient<OpenAiRecipeSuggestionClient>();
builder.Services.Configure<RecipeAiOptions>(builder.Configuration.GetSection(RecipeAiOptions.SectionName));
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

DishesCreateEndpoint.Map(app);
DishesDeleteEndpoint.Map(app);
DishesImportEndpoint.Map(app);
DishesLookupEndpoint.Map(app);
DishesMetadataEndpoint.Map(app);
DishesOverviewEndpoint.Map(app);
DishesUpdateEndpoint.Map(app);
IngredientsCreateEndpoint.Map(app);
IngredientsDeleteEndpoint.Map(app);
IngredientsMetadataEndpoint.Map(app);
IngredientsOverviewEndpoint.Map(app);
IngredientsUpdateEndpoint.Map(app);
RecipesInstructionsEndpoint.Map(app);
RecipesSuggestionsEndpoint.Map(app);
PlanningSettingsGetEndpoint.Map(app);
PlanningSettingsUpsertEndpoint.Map(app);
ShoppingListByStartDateEndpoint.Map(app);
WeeklyPlansAvailableEndpoint.Map(app);
WeeklyPlansByStartDateEndpoint.Map(app);
WeeklyPlansGenerateEndpoint.Map(app);
WeeklyPlansMarkEatenEndpoint.Map(app);
PlannedDishByDateEndpoint.Map(app);
WeeklyPlansUpsertEndpoint.Map(app);

app.Run();
