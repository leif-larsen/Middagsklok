using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Middagsklok.Api.Features.Recipes.Suggestions;

internal sealed class ClaudeRecipeSuggestionClient(
    HttpClient httpClient,
    IOptions<RecipeAiOptions> options) : IRecipeSuggestionClient
{
    private const string AnthropicVersion = "2023-06-01";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient = httpClient;
    private readonly IOptions<RecipeAiOptions> _options = options;

    public string ProviderName => "Claude";

    // Sends a prompt to Claude and parses structured suggestions.
    public async Task<RecipeSuggestionGenerationResult> Generate(
        RecipeSuggestionGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var claude = _options.Value.Claude;
        var apiKey = claude.ApiKey?.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            var missingKey = RecipeSuggestionGenerationResult.Failure(
                "Claude API key is missing. Set Ai:Claude:ApiKey.");

            return missingKey;
        }

        var model = claude.Model?.Trim();
        if (string.IsNullOrWhiteSpace(model))
        {
            var missingModel = RecipeSuggestionGenerationResult.Failure(
                "Claude model is missing. Set Ai:Claude:Model.");

            return missingModel;
        }

        var timeoutSeconds = Math.Clamp(claude.TimeoutSeconds, 5, 120);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            var endpoint = BuildEndpoint(claude.BaseUrl);
            var payload = BuildPayload(request, model);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(payload)
            };
            httpRequest.Headers.Add("x-api-key", apiKey);
            httpRequest.Headers.Add("anthropic-version", AnthropicVersion);

            var httpResponse = await _httpClient.SendAsync(httpRequest, timeoutCts.Token);
            var body = await httpResponse.Content.ReadAsStringAsync(timeoutCts.Token);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var failed = RecipeSuggestionGenerationResult.Failure(
                    $"Claude request failed with {(int)httpResponse.StatusCode}: {body}");

                return failed;
            }

            var parsedResponse = JsonSerializer.Deserialize<ClaudeMessagesResponse>(body, JsonOptions);
            var content = ExtractTextContent(parsedResponse);
            if (string.IsNullOrWhiteSpace(content))
            {
                var emptyContent = RecipeSuggestionGenerationResult.Failure("Claude returned an empty response.");

                return emptyContent;
            }

            var suggestions = ParseSuggestions(content, request.MaxSuggestions);
            if (suggestions.Count == 0)
            {
                var invalidContent = RecipeSuggestionGenerationResult.Failure(
                    "Claude response did not contain valid suggestions.");

                return invalidContent;
            }

            var success = RecipeSuggestionGenerationResult.Success(suggestions);

            return success;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            var timeout = RecipeSuggestionGenerationResult.Failure("Claude request timed out.");

            return timeout;
        }
        catch (HttpRequestException ex)
        {
            var transportError = RecipeSuggestionGenerationResult.Failure($"Claude request failed: {ex.Message}");

            return transportError;
        }
        catch (JsonException ex)
        {
            var parseError = RecipeSuggestionGenerationResult.Failure($"Claude response parsing failed: {ex.Message}");

            return parseError;
        }
    }

    // Builds the Claude messages API endpoint URI.
    private static Uri BuildEndpoint(string? configuredBaseUrl)
    {
        var baseUrl = string.IsNullOrWhiteSpace(configuredBaseUrl)
            ? "https://api.anthropic.com/v1"
            : configuredBaseUrl.Trim();

        var normalizedBaseUrl = baseUrl.EndsWith('/')
            ? baseUrl
            : $"{baseUrl}/";

        var endpoint = new Uri(new Uri(normalizedBaseUrl, UriKind.Absolute), "messages");

        return endpoint;
    }

    // Builds the Claude messages payload.
    private static ClaudeMessagesRequest BuildPayload(
        RecipeSuggestionGenerationRequest request,
        string model)
    {
        var systemMessage = "You are a meal-planning assistant. Return only valid JSON in this shape: { \"suggestions\": [{ \"title\": string, \"summary\": string, \"reason\": string|null, \"estimatedTotalMinutes\": number|null }] }. Keep suggestions practical and concise.";
        var userMessage = BuildUserMessage(request);

        var payload = new ClaudeMessagesRequest(
            model,
            systemMessage,
            [new ClaudeMessage("user", userMessage)],
            4096);

        return payload;
    }

    // Builds a user message with prompt and available dish context.
    private static string BuildUserMessage(RecipeSuggestionGenerationRequest request)
    {
        var dishesContext = request.KnownDishes
            .Take(30)
            .Select(dish =>
                $"- {dish.Name} ({dish.DishType}), totalMinutes={dish.TotalMinutes}, tags=[{string.Join(", ", dish.VibeTags)}], seafood={dish.IsSeafood}, vegetarian={dish.IsVegetarian}, vegan={dish.IsVegan}")
            .ToArray();

        var dishesSection = dishesContext.Length == 0
            ? "No existing dishes available."
            : string.Join("\n", dishesContext);

        var message = $"Prompt: {request.Prompt}\nDesired suggestions: {request.MaxSuggestions}\nKnown dishes for inspiration:\n{dishesSection}";

        return message;
    }

    // Extracts text content from Claude response.
    private static string? ExtractTextContent(ClaudeMessagesResponse? response)
    {
        if (response?.Content is null)
        {
            return null;
        }

        var textContent = response.Content
            .Where(block => block.Type == "text")
            .Select(block => block.Text)
            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));

        return textContent;
    }

    // Parses model output into normalized suggestion records.
    private static IReadOnlyList<GeneratedRecipeSuggestion> ParseSuggestions(string content, int maxSuggestions)
    {
        using var document = ParseJsonDocument(content);
        var root = document.RootElement;

        if (!root.TryGetProperty("suggestions", out var suggestionsNode)
            || suggestionsNode.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<GeneratedRecipeSuggestion>();
        }

        var suggestions = new List<GeneratedRecipeSuggestion>();

        foreach (var node in suggestionsNode.EnumerateArray())
        {
            var title = node.TryGetProperty("title", out var titleNode)
                ? titleNode.GetString()?.Trim()
                : null;
            var summary = node.TryGetProperty("summary", out var summaryNode)
                ? summaryNode.GetString()?.Trim()
                : null;

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(summary))
            {
                continue;
            }

            var reason = node.TryGetProperty("reason", out var reasonNode)
                ? reasonNode.GetString()?.Trim()
                : null;
            var estimatedTotalMinutes = node.TryGetProperty("estimatedTotalMinutes", out var etaNode)
                && etaNode.TryGetInt32(out var etaValue)
                ? (int?)etaValue
                : null;

            var suggestion = new GeneratedRecipeSuggestion(
                $"ai-{suggestions.Count + 1}",
                title,
                summary,
                string.IsNullOrWhiteSpace(reason) ? null : reason,
                estimatedTotalMinutes);

            suggestions.Add(suggestion);

            if (suggestions.Count >= maxSuggestions)
            {
                break;
            }
        }

        return suggestions;
    }

    // Parses response content and tolerates wrapped JSON blocks.
    private static JsonDocument ParseJsonDocument(string content)
    {
        var trimmed = content.Trim();

        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstBrace = trimmed.IndexOf('{');
            var lastBrace = trimmed.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                trimmed = trimmed[firstBrace..(lastBrace + 1)];
            }
        }

        var document = JsonDocument.Parse(trimmed);

        return document;
    }

    private sealed record ClaudeMessagesRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("system")] string System,
        [property: JsonPropertyName("messages")] IReadOnlyList<ClaudeMessage> Messages,
        [property: JsonPropertyName("max_tokens")] int MaxTokens);

    private sealed record ClaudeMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ClaudeMessagesResponse(
        [property: JsonPropertyName("content")] IReadOnlyList<ClaudeContentBlock>? Content);

    private sealed record ClaudeContentBlock(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("text")] string? Text);
}
