namespace Middagsklok.Api.Features.Recipes.Suggestions;

public sealed record Response(IReadOnlyList<RecipeSuggestion> Suggestions);

public sealed record RecipeSuggestion(
    string Id,
    string Title,
    string Summary,
    string? Reason,
    int? EstimatedTotalMinutes);

public sealed record ErrorResponse(
    string Message,
    IReadOnlyList<ValidationError> Errors);

public sealed record ValidationError(string Field, string Message);
