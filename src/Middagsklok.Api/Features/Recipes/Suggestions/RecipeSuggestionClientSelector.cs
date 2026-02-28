using Microsoft.Extensions.Options;

namespace Middagsklok.Api.Features.Recipes.Suggestions;

internal sealed class RecipeSuggestionClientSelector(
    IOptions<RecipeAiOptions> options,
    OpenAiRecipeSuggestionClient openAiClient,
    ClaudeRecipeSuggestionClient claudeClient) : IRecipeSuggestionClientSelector
{
    private readonly IOptions<RecipeAiOptions> _options = options;
    private readonly OpenAiRecipeSuggestionClient _openAiClient = openAiClient;
    private readonly ClaudeRecipeSuggestionClient _claudeClient = claudeClient;

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

        if (provider.Equals("Claude", StringComparison.OrdinalIgnoreCase))
        {
            var claudeSelection = RecipeSuggestionClientSelection.Success(_claudeClient);

            return claudeSelection;
        }

        if (provider.Equals("GitHubModels", StringComparison.OrdinalIgnoreCase)
            || provider.Equals("Foundry", StringComparison.OrdinalIgnoreCase))
        {
            var unsupportedProvider = RecipeSuggestionClientSelection.Failure(
                $"Configured provider '{provider}' is not implemented yet. Currently supported providers: OpenAI, Claude.");

            return unsupportedProvider;
        }

        var unknownProvider = RecipeSuggestionClientSelection.Failure(
            $"Unknown provider '{provider}'. Allowed values: OpenAI, Claude, GitHubModels, Foundry.");

        return unknownProvider;
    }
}
