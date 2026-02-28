using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Middagsklok.Api.Features.Recipes.Suggestions;

internal sealed class OpenAiRecipeSuggestionClient(
    HttpClient httpClient,
    IOptions<RecipeAiOptions> options) : IRecipeSuggestionClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient = httpClient;
    private readonly IOptions<RecipeAiOptions> _options = options;

    public string ProviderName => "OpenAI";

    // Sends a prompt to OpenAI and parses structured suggestions.
    public async Task<RecipeSuggestionGenerationResult> Generate(
        RecipeSuggestionGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var openAi = _options.Value.OpenAi;
        var apiKey = openAi.ApiKey?.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            var missingKey = RecipeSuggestionGenerationResult.Failure(
                "OpenAI API key is missing. Set Ai:OpenAi:ApiKey.");

            return missingKey;
        }

        var model = openAi.Model?.Trim();
        if (string.IsNullOrWhiteSpace(model))
        {
            var missingModel = RecipeSuggestionGenerationResult.Failure(
                "OpenAI model is missing. Set Ai:OpenAi:Model.");

            return missingModel;
        }

        var timeoutSeconds = Math.Clamp(openAi.TimeoutSeconds, 5, 120);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            var endpoint = BuildEndpoint(openAi.BaseUrl);
            var payload = BuildPayload(request, model);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(payload)
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var httpResponse = await _httpClient.SendAsync(httpRequest, timeoutCts.Token);
            var body = await httpResponse.Content.ReadAsStringAsync(timeoutCts.Token);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var failed = RecipeSuggestionGenerationResult.Failure(
                    $"OpenAI request failed with {(int)httpResponse.StatusCode}: {body}");

                return failed;
            }

            var parsedResponse = JsonSerializer.Deserialize<OpenAiChatCompletionResponse>(body, JsonOptions);
            var content = parsedResponse?.Choices?.FirstOrDefault()?.Message?.Content;
            if (string.IsNullOrWhiteSpace(content))
            {
                var emptyContent = RecipeSuggestionGenerationResult.Failure("OpenAI returned an empty response.");

                return emptyContent;
            }

            var suggestions = ParseSuggestions(content, request.MaxSuggestions);
            if (suggestions.Count == 0)
            {
                var invalidContent = RecipeSuggestionGenerationResult.Failure(
                    "OpenAI response did not contain valid suggestions.");

                return invalidContent;
            }

            var success = RecipeSuggestionGenerationResult.Success(suggestions);

            return success;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            var timeout = RecipeSuggestionGenerationResult.Failure("OpenAI request timed out.");

            return timeout;
        }
        catch (HttpRequestException ex)
        {
            var transportError = RecipeSuggestionGenerationResult.Failure($"OpenAI request failed: {ex.Message}");

            return transportError;
        }
        catch (JsonException ex)
        {
            var parseError = RecipeSuggestionGenerationResult.Failure($"OpenAI response parsing failed: {ex.Message}");

            return parseError;
        }
    }

    // Builds the OpenAI endpoint URI.
    private static Uri BuildEndpoint(string? configuredBaseUrl)
    {
        var baseUrl = string.IsNullOrWhiteSpace(configuredBaseUrl)
            ? "https://api.openai.com/v1"
            : configuredBaseUrl.Trim();

        var normalizedBaseUrl = baseUrl.EndsWith('/')
            ? baseUrl
            : $"{baseUrl}/";

        var endpoint = new Uri(new Uri(normalizedBaseUrl, UriKind.Absolute), "chat/completions");

        return endpoint;
    }

    // Builds the model input payload.
    private static OpenAiChatCompletionRequest BuildPayload(
        RecipeSuggestionGenerationRequest request,
        string model)
    {
        var systemMessage = "You are a meal-planning assistant that suggests NEW recipes the user doesn't already have. You must suggest dishes that are DIFFERENT from the user's existing dishes. Use their existing dishes only to understand their cooking style and preferences, then suggest completely new recipes they haven't tried. Return only valid JSON in this shape: { \"suggestions\": [{ \"title\": string, \"summary\": string, \"reason\": string|null, \"estimatedTotalMinutes\": number|null }] }. Keep suggestions practical and concise.";
        var userMessage = BuildUserMessage(request);

        var payload = new OpenAiChatCompletionRequest(
            model,
            [
                new OpenAiChatMessage("system", systemMessage),
                new OpenAiChatMessage("user", userMessage)
            ],
            0.4,
            new OpenAiResponseFormat("json_object"));

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
            ? "No existing dishes in database."
            : string.Join("\n", dishesContext);

        var message = $"Prompt: {request.Prompt}\nDesired suggestions: {request.MaxSuggestions}\n\nExisting dishes in my database (DO NOT suggest these or variations of these - suggest completely NEW dishes instead):\n{dishesSection}";

        return message;
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

    private sealed record OpenAiChatCompletionRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<OpenAiChatMessage> Messages,
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("response_format")] OpenAiResponseFormat ResponseFormat);

    private sealed record OpenAiChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record OpenAiResponseFormat(
        [property: JsonPropertyName("type")] string Type);

    private sealed record OpenAiChatCompletionResponse(
        [property: JsonPropertyName("choices")] IReadOnlyList<OpenAiChoice>? Choices);

    private sealed record OpenAiChoice(
        [property: JsonPropertyName("message")] OpenAiMessage? Message);

    private sealed record OpenAiMessage(
        [property: JsonPropertyName("content")] string? Content);
}
