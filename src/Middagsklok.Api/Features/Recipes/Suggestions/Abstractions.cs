namespace Middagsklok.Api.Features.Recipes.Suggestions;

internal interface IRecipeSuggestionClient
{
    string ProviderName { get; }

    // Generates recipe suggestions for a normalized request.
    Task<RecipeSuggestionGenerationResult> Generate(
        RecipeSuggestionGenerationRequest request,
        CancellationToken cancellationToken);
}

internal interface IRecipeSuggestionClientSelector
{
    // Selects the active provider implementation from configuration.
    RecipeSuggestionClientSelection Select();
}

internal sealed record RecipeSuggestionGenerationRequest(
    string Prompt,
    int MaxSuggestions,
    IReadOnlyList<DishContext> KnownDishes);

internal sealed record DishContext(
    string Name,
    string DishType,
    bool IsSeafood,
    bool IsVegetarian,
    bool IsVegan,
    IReadOnlyList<string> VibeTags,
    int TotalMinutes);

internal sealed record GeneratedRecipeSuggestion(
    string Id,
    string Title,
    string Summary,
    string? Reason,
    int? EstimatedTotalMinutes);

internal sealed record RecipeSuggestionGenerationResult(
    bool IsSuccess,
    IReadOnlyList<GeneratedRecipeSuggestion> Suggestions,
    string? ErrorMessage)
{
    // Creates a successful generation result.
    public static RecipeSuggestionGenerationResult Success(IReadOnlyList<GeneratedRecipeSuggestion> suggestions)
    {
        var result = new RecipeSuggestionGenerationResult(true, suggestions, null);

        return result;
    }

    // Creates a failed generation result.
    public static RecipeSuggestionGenerationResult Failure(string errorMessage)
    {
        var result = new RecipeSuggestionGenerationResult(false, Array.Empty<GeneratedRecipeSuggestion>(), errorMessage);

        return result;
    }
}

internal sealed record RecipeSuggestionClientSelection(
    bool IsSuccess,
    IRecipeSuggestionClient? Client,
    string? ErrorMessage)
{
    // Creates a successful provider selection.
    public static RecipeSuggestionClientSelection Success(IRecipeSuggestionClient client)
    {
        var result = new RecipeSuggestionClientSelection(true, client, null);

        return result;
    }

    // Creates a failed provider selection.
    public static RecipeSuggestionClientSelection Failure(string errorMessage)
    {
        var result = new RecipeSuggestionClientSelection(false, null, errorMessage);

        return result;
    }
}
