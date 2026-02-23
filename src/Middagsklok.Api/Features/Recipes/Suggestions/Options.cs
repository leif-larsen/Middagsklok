namespace Middagsklok.Api.Features.Recipes.Suggestions;

internal sealed class RecipeAiOptions
{
    public const string SectionName = "Ai";

    public string Provider { get; init; } = "OpenAI";
    public OpenAiOptions OpenAi { get; init; } = new();
    public ClaudeOptions Claude { get; init; } = new();
    public GitHubModelsOptions GitHubModels { get; init; } = new();
    public FoundryOptions Foundry { get; init; } = new();
}

internal sealed class OpenAiOptions
{
    public string BaseUrl { get; init; } = "https://api.openai.com/v1";
    public string? ApiKey { get; init; }
    public string Model { get; init; } = "gpt-4o-mini";
    public int TimeoutSeconds { get; init; } = 30;
}

internal sealed class ClaudeOptions
{
    public string? ApiKey { get; init; }
    public string Model { get; init; } = "claude-3-5-sonnet-latest";
}

internal sealed class GitHubModelsOptions
{
    public string? ApiKey { get; init; }
    public string Endpoint { get; init; } = "https://models.inference.ai.azure.com";
    public string Model { get; init; } = "gpt-4o-mini";
}

internal sealed class FoundryOptions
{
    public string? Endpoint { get; init; }
    public string? ApiKey { get; init; }
    public string? Deployment { get; init; }
    public string ApiVersion { get; init; } = "2024-10-21";
}
