using Microsoft.Extensions.Options;

namespace Middagsklok.Api.Features.Recipes.Suggestions;

internal sealed class RecipeSuggestionClientSelector(
    IOptions<RecipeAiOptions> options,
    OpenAiRecipeSuggestionClient openAiClient) : IRecipeSuggestionClientSelector
{
    private readonly IOptions<RecipeAiOptions> _options = options;
    private readonly OpenAiRecipeSuggestionClient _openAiClient = openAiClient;

    // Selects the configured provider implementation.
    public RecipeSuggestionClientSelection Select()
    {
        var provider = _options.Value.Provider?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(provider))
        {
            var emptyProvider = RecipeSuggestionClientSelection.Failure(
                "AI provider is not configured. Set Ai:Provider to OpenAI.");

            return emptyProvider;
        }

        if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            var openAiSelection = RecipeSuggestionClientSelection.Success(_openAiClient);

            return openAiSelection;
        }

        if (provider.Equals("Claude", StringComparison.OrdinalIgnoreCase)
            || provider.Equals("GitHubModels", StringComparison.OrdinalIgnoreCase)
            || provider.Equals("Foundry", StringComparison.OrdinalIgnoreCase))
        {
            var unsupportedProvider = RecipeSuggestionClientSelection.Failure(
                $"Configured provider '{provider}' is not implemented yet. Currently supported provider: OpenAI.");

            return unsupportedProvider;
        }

        var unknownProvider = RecipeSuggestionClientSelection.Failure(
            $"Configured provider '{provider}' is unknown. Allowed values: OpenAI, Claude, GitHubModels, Foundry.");

        return unknownProvider;
    }
}
