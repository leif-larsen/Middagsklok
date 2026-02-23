using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;

namespace Middagsklok.Api.Features.Recipes.Suggestions;

internal sealed class UseCase(
    AppDbContext dbContext,
    IRecipeSuggestionClientSelector selector)
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly IRecipeSuggestionClientSelector _selector = selector;

    // Executes the recipe suggestion workflow.
    public async Task<UseCaseResult> Execute(Request? request, CancellationToken cancellationToken)
    {
        var validator = new Validator();
        var validation = validator.Validate(request);

        if (!validation.IsValid || validation.Candidate is null)
        {
            var invalid = new UseCaseResult(SuggestionsOutcome.Invalid, null, validation.Errors);

            return invalid;
        }

        var selection = _selector.Select();
        if (!selection.IsSuccess || selection.Client is null)
        {
            var errors = new[]
            {
                new ValidationError("provider", selection.ErrorMessage ?? "AI provider is unavailable.")
            };
            var unavailable = new UseCaseResult(SuggestionsOutcome.Unavailable, null, errors);

            return unavailable;
        }

        var knownDishes = await LoadDishContext(cancellationToken);
        var generationRequest = new RecipeSuggestionGenerationRequest(
            validation.Candidate.Prompt,
            validation.Candidate.MaxSuggestions,
            knownDishes);

        var generation = await selection.Client.Generate(generationRequest, cancellationToken);
        if (!generation.IsSuccess)
        {
            var errors = new[]
            {
                new ValidationError("provider", generation.ErrorMessage ?? "Unable to generate suggestions.")
            };
            var unavailable = new UseCaseResult(SuggestionsOutcome.Unavailable, null, errors);

            return unavailable;
        }

        var suggestions = generation.Suggestions
            .Select(item => new RecipeSuggestion(
                item.Id,
                item.Title,
                item.Summary,
                item.Reason,
                item.EstimatedTotalMinutes))
            .ToArray();

        var response = new Response(suggestions);
        var success = new UseCaseResult(SuggestionsOutcome.Success, response, Array.Empty<ValidationError>());

        return success;
    }

    // Loads dish context to ground model suggestions.
    private async Task<IReadOnlyList<DishContext>> LoadDishContext(CancellationToken cancellationToken)
    {
        var dishes = await _dbContext.Dishes
            .AsNoTracking()
            .OrderBy(dish => dish.Name)
            .Select(dish => new DishContext(
                dish.Name,
                dish.DishType.ToString(),
                dish.IsSeafood,
                dish.IsVegetarian,
                dish.IsVegan,
                dish.VibeTags.ToArray(),
                dish.TotalTimeMinutes))
            .ToListAsync(cancellationToken);

        return dishes;
    }
}

internal enum SuggestionsOutcome
{
    Success,
    Invalid,
    Unavailable
}

internal sealed record UseCaseResult(
    SuggestionsOutcome Outcome,
    Response? Response,
    IReadOnlyList<ValidationError> Errors);
