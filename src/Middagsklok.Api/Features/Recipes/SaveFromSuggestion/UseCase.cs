using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Dish;
using Middagsklok.Api.Domain.Ingredient;
using Middagsklok.Api.Features.Recipes.Suggestions;

namespace Middagsklok.Api.Features.Recipes.SaveFromSuggestion;

internal sealed class UseCase(
    AppDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    IOptions<RecipeAiOptions> options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _dbContext = dbContext;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IOptions<RecipeAiOptions> _options = options;

    // Executes the workflow to expand and save an AI suggestion as a dish.
    public async Task<UseCaseResult> Execute(Request? request, CancellationToken cancellationToken)
    {
        var validator = new Validator();
        var validation = validator.Validate(request);

        if (!validation.IsValid || validation.Candidate is null)
        {
            var invalidResult = new UseCaseResult(SaveOutcome.Invalid, null, validation.Errors);

            return invalidResult;
        }

        var normalizedName = NormalizeName(validation.Candidate.Title);
        var hasDuplicate = await _dbContext.Dishes
            .AsNoTracking()
            .AnyAsync(dish => dish.Name.ToUpper() == normalizedName, cancellationToken);

        if (hasDuplicate)
        {
            var conflictError = new ValidationError("title", $"Dish '{validation.Candidate.Title}' already exists.");
            var conflictResult = new UseCaseResult(SaveOutcome.Conflict, null, new[] { conflictError });

            return conflictResult;
        }

        var expandedRecipe = await ExpandSuggestion(validation.Candidate, cancellationToken);
        if (!expandedRecipe.IsSuccess || expandedRecipe.Recipe is null)
        {
            var aiError = new ValidationError("ai", expandedRecipe.ErrorMessage ?? "Failed to generate recipe details.");
            var unavailableResult = new UseCaseResult(SaveOutcome.Unavailable, null, new[] { aiError });

            return unavailableResult;
        }

        var recipe = expandedRecipe.Recipe;
        var ingredientsByName = await LoadIngredientsByName(recipe.Ingredients, cancellationToken);
        var ingredientLookup = new Dictionary<Guid, Ingredient>();
        var dishIngredients = new List<DishIngredient>();

        var sortOrder = 1;
        foreach (var ingredientData in recipe.Ingredients)
        {
            var ingredient = ResolveIngredient(ingredientData, ingredientsByName);
            ingredientLookup[ingredient.Id] = ingredient;

            var dishIngredient = new DishIngredient(
                ingredient.Id,
                ingredientData.Quantity,
                ParseUnit(ingredientData.Unit),
                null,
                sortOrder++);

            dishIngredients.Add(dishIngredient);
        }

        var dish = new Dish(
            validation.Candidate.Title,
            ParseDishType(recipe.DishType),
            recipe.PrepTimeMinutes,
            recipe.CookTimeMinutes,
            recipe.Servings,
            recipe.Instructions,
            recipe.IsSeafood,
            recipe.IsVegetarian,
            recipe.IsVegan,
            dishIngredients,
            recipe.VibeTags);

        _dbContext.Dishes.Add(dish);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = MapDish(dish, ingredientLookup);
        var successResult = new UseCaseResult(SaveOutcome.Success, response, Array.Empty<ValidationError>());

        return successResult;
    }

    // Calls the AI provider to expand a suggestion into a full recipe.
    private async Task<RecipeExpansionResult> ExpandSuggestion(
        ValidatedCandidate candidate,
        CancellationToken cancellationToken)
    {
        var provider = _options.Value.Provider?.Trim() ?? string.Empty;

        return provider.ToUpperInvariant() switch
        {
            "OPENAI" => await ExpandWithOpenAi(candidate, cancellationToken),
            "CLAUDE" => await ExpandWithClaude(candidate, cancellationToken),
            _ => RecipeExpansionResult.Failure($"Configured provider '{provider}' is not supported for recipe expansion.")
        };
    }

    // Expands a suggestion using the OpenAI API.
    private async Task<RecipeExpansionResult> ExpandWithOpenAi(
        ValidatedCandidate candidate,
        CancellationToken cancellationToken)
    {
        var openAi = _options.Value.OpenAi;
        var apiKey = openAi.ApiKey?.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return RecipeExpansionResult.Failure("OpenAI API key is missing.");
        }

        var model = openAi.Model?.Trim();
        if (string.IsNullOrWhiteSpace(model))
        {
            return RecipeExpansionResult.Failure("OpenAI model is missing.");
        }

        var timeoutSeconds = Math.Clamp(openAi.TimeoutSeconds, 5, 120);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            var baseUrl = string.IsNullOrWhiteSpace(openAi.BaseUrl)
                ? "https://api.openai.com/v1"
                : openAi.BaseUrl.Trim();
            var normalizedBaseUrl = baseUrl.EndsWith('/') ? baseUrl : $"{baseUrl}/";
            var endpoint = new Uri(new Uri(normalizedBaseUrl, UriKind.Absolute), "chat/completions");

            var payload = BuildOpenAiPayload(candidate, model);
            using var httpClient = _httpClientFactory.CreateClient();
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(payload)
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var httpResponse = await httpClient.SendAsync(httpRequest, timeoutCts.Token);
            var body = await httpResponse.Content.ReadAsStringAsync(timeoutCts.Token);

            if (!httpResponse.IsSuccessStatusCode)
            {
                return RecipeExpansionResult.Failure($"OpenAI request failed: {body}");
            }

            var parsedResponse = JsonSerializer.Deserialize<OpenAiChatCompletionResponse>(body, JsonOptions);
            var content = parsedResponse?.Choices?.FirstOrDefault()?.Message?.Content;

            return ParseExpandedRecipe(content);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return RecipeExpansionResult.Failure("OpenAI request timed out.");
        }
        catch (Exception ex)
        {
            return RecipeExpansionResult.Failure($"OpenAI request failed: {ex.Message}");
        }
    }

    // Expands a suggestion using the Claude API.
    private async Task<RecipeExpansionResult> ExpandWithClaude(
        ValidatedCandidate candidate,
        CancellationToken cancellationToken)
    {
        var claude = _options.Value.Claude;
        var apiKey = claude.ApiKey?.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return RecipeExpansionResult.Failure("Claude API key is missing.");
        }

        var model = claude.Model?.Trim();
        if (string.IsNullOrWhiteSpace(model))
        {
            return RecipeExpansionResult.Failure("Claude model is missing.");
        }

        var timeoutSeconds = Math.Clamp(claude.TimeoutSeconds, 5, 120);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            var baseUrl = string.IsNullOrWhiteSpace(claude.BaseUrl)
                ? "https://api.anthropic.com/v1"
                : claude.BaseUrl.Trim();
            var normalizedBaseUrl = baseUrl.EndsWith('/') ? baseUrl : $"{baseUrl}/";
            var endpoint = new Uri(new Uri(normalizedBaseUrl, UriKind.Absolute), "messages");

            var payload = BuildClaudePayload(candidate, model);
            using var httpClient = _httpClientFactory.CreateClient();
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(payload)
            };
            httpRequest.Headers.Add("x-api-key", apiKey);
            httpRequest.Headers.Add("anthropic-version", "2023-06-01");

            var httpResponse = await httpClient.SendAsync(httpRequest, timeoutCts.Token);
            var body = await httpResponse.Content.ReadAsStringAsync(timeoutCts.Token);

            if (!httpResponse.IsSuccessStatusCode)
            {
                return RecipeExpansionResult.Failure($"Claude request failed: {body}");
            }

            var parsedResponse = JsonSerializer.Deserialize<ClaudeMessagesResponse>(body, JsonOptions);
            var content = parsedResponse?.Content?
                .Where(block => block.Type == "text")
                .Select(block => block.Text)
                .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));

            return ParseExpandedRecipe(content);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return RecipeExpansionResult.Failure("Claude request timed out.");
        }
        catch (Exception ex)
        {
            return RecipeExpansionResult.Failure($"Claude request failed: {ex.Message}");
        }
    }

    // Builds the OpenAI request payload for recipe expansion.
    private static object BuildOpenAiPayload(ValidatedCandidate candidate, string model)
    {
        var systemMessage = BuildSystemPrompt();
        var userMessage = BuildUserPrompt(candidate);

        return new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = systemMessage },
                new { role = "user", content = userMessage }
            },
            temperature = 0.4,
            response_format = new { type = "json_object" }
        };
    }

    // Builds the Claude request payload for recipe expansion.
    private static object BuildClaudePayload(ValidatedCandidate candidate, string model)
    {
        var systemMessage = BuildSystemPrompt();
        var userMessage = BuildUserPrompt(candidate);

        return new
        {
            model,
            system = systemMessage,
            messages = new[]
            {
                new { role = "user", content = userMessage }
            },
            max_tokens = 4096
        };
    }

    // Builds the system prompt for recipe expansion.
    private static string BuildSystemPrompt()
    {
        return """
            You are a recipe expansion assistant. Given a dish title and summary, generate a complete recipe with exact details.
            Return ONLY valid JSON in this exact shape:
            {
              "dishType": string (one of: Pasta, RiceBowl, Noodles, SoupStew, Salad, WrapTaco, PizzaPie, CasseroleBake, SandwichBurger, ProteinVegPlate, BreakfastDinner, SnackBoard, Other),
              "prepTimeMinutes": number,
              "cookTimeMinutes": number,
              "servings": number,
              "isSeafood": boolean,
              "isVegetarian": boolean,
              "isVegan": boolean,
              "vibeTags": string[] (e.g., ["quick", "healthy", "comfort food"]),
              "instructions": string (detailed step-by-step instructions as a single text block, each step on a new line),
              "ingredients": [{ "name": string, "quantity": number, "unit": string (one of: g, kg, ml, l, pcs) }]
            }
            Be practical, use common ingredients, and provide clear instructions.
            """;
    }

    // Builds the user prompt for recipe expansion.
    private static string BuildUserPrompt(ValidatedCandidate candidate)
    {
        var totalMinutes = candidate.EstimatedTotalMinutes.HasValue
            ? $"\nEstimated total time: {candidate.EstimatedTotalMinutes} minutes"
            : string.Empty;

        return $"""
            Please generate a complete recipe for:
            Title: {candidate.Title}
            Summary: {candidate.Summary}{totalMinutes}
            """;
    }

    // Parses the AI response into an expanded recipe.
    private static RecipeExpansionResult ParseExpandedRecipe(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return RecipeExpansionResult.Failure("AI returned an empty response.");
        }

        try
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

            using var document = JsonDocument.Parse(trimmed);
            var root = document.RootElement;

            var dishType = root.TryGetProperty("dishType", out var dishTypeNode)
                ? dishTypeNode.GetString() ?? "Other"
                : "Other";
            var prepTimeMinutes = root.TryGetProperty("prepTimeMinutes", out var prepNode) && prepNode.TryGetInt32(out var prep)
                ? prep
                : 15;
            var cookTimeMinutes = root.TryGetProperty("cookTimeMinutes", out var cookNode) && cookNode.TryGetInt32(out var cook)
                ? cook
                : 30;
            var servings = root.TryGetProperty("servings", out var servingsNode) && servingsNode.TryGetInt32(out var srv)
                ? srv
                : 4;
            var isSeafood = root.TryGetProperty("isSeafood", out var seafoodNode) && seafoodNode.GetBoolean();
            var isVegetarian = root.TryGetProperty("isVegetarian", out var vegNode) && vegNode.GetBoolean();
            var isVegan = root.TryGetProperty("isVegan", out var veganNode) && veganNode.GetBoolean();
            var instructions = root.TryGetProperty("instructions", out var instNode)
                ? instNode.GetString()
                : null;

            var vibeTags = new List<string>();
            if (root.TryGetProperty("vibeTags", out var tagsNode) && tagsNode.ValueKind == JsonValueKind.Array)
            {
                foreach (var tag in tagsNode.EnumerateArray())
                {
                    var tagValue = tag.GetString();
                    if (!string.IsNullOrWhiteSpace(tagValue))
                    {
                        vibeTags.Add(tagValue);
                    }
                }
            }

            var ingredients = new List<ExpandedIngredient>();
            if (root.TryGetProperty("ingredients", out var ingredientsNode) && ingredientsNode.ValueKind == JsonValueKind.Array)
            {
                foreach (var ingredientNode in ingredientsNode.EnumerateArray())
                {
                    var name = ingredientNode.TryGetProperty("name", out var nameNode)
                        ? nameNode.GetString()?.Trim()
                        : null;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    var quantity = ingredientNode.TryGetProperty("quantity", out var qtyNode) && qtyNode.TryGetDouble(out var qty)
                        ? qty
                        : 1;
                    var unit = ingredientNode.TryGetProperty("unit", out var unitNode)
                        ? unitNode.GetString() ?? "pcs"
                        : "pcs";

                    ingredients.Add(new ExpandedIngredient(name, quantity, unit));
                }
            }

            if (ingredients.Count == 0)
            {
                return RecipeExpansionResult.Failure("AI response did not contain any ingredients.");
            }

            var recipe = new ExpandedRecipe(
                dishType,
                prepTimeMinutes,
                cookTimeMinutes,
                servings,
                isSeafood,
                isVegetarian,
                isVegan,
                vibeTags,
                instructions,
                ingredients);

            return RecipeExpansionResult.Success(recipe);
        }
        catch (JsonException ex)
        {
            return RecipeExpansionResult.Failure($"Failed to parse AI response: {ex.Message}");
        }
    }

    // Loads existing ingredients by normalized name.
    private async Task<Dictionary<string, Ingredient>> LoadIngredientsByName(
        IReadOnlyList<ExpandedIngredient> ingredients,
        CancellationToken cancellationToken)
    {
        var names = ingredients
            .Select(ingredient => NormalizeName(ingredient.Name))
            .Distinct()
            .ToArray();

        if (names.Length == 0)
        {
            return new Dictionary<string, Ingredient>(StringComparer.OrdinalIgnoreCase);
        }

        var items = await _dbContext.Ingredients
            .Where(ingredient => names.Contains(ingredient.Name.ToUpper()))
            .ToListAsync(cancellationToken);

        var lookup = items.ToDictionary(
            ingredient => NormalizeName(ingredient.Name),
            ingredient => ingredient,
            StringComparer.OrdinalIgnoreCase);

        return lookup;
    }

    // Resolves an ingredient by name, creating it if necessary.
    private Ingredient ResolveIngredient(
        ExpandedIngredient ingredientData,
        IDictionary<string, Ingredient> ingredientsByName)
    {
        var normalizedName = NormalizeName(ingredientData.Name);

        if (ingredientsByName.TryGetValue(normalizedName, out var existing))
        {
            return existing;
        }

        var category = InferCategory(ingredientData.Name);
        var unit = ParseUnit(ingredientData.Unit);
        var ingredient = new Ingredient(ingredientData.Name, category, unit);
        ingredientsByName[normalizedName] = ingredient;
        _dbContext.Ingredients.Add(ingredient);

        return ingredient;
    }

    // Infers ingredient category from its name.
    private static IngredientCategory InferCategory(string name)
    {
        var lowerName = name.ToLowerInvariant();

        if (lowerName.Contains("chicken") || lowerName.Contains("beef") || lowerName.Contains("pork") || lowerName.Contains("lamb"))
        {
            return IngredientCategory.Meat;
        }

        if (lowerName.Contains("fish") || lowerName.Contains("salmon") || lowerName.Contains("shrimp") || lowerName.Contains("tuna"))
        {
            return IngredientCategory.Seafood;
        }

        if (lowerName.Contains("milk") || lowerName.Contains("cheese") || lowerName.Contains("butter") || lowerName.Contains("cream") || lowerName.Contains("egg"))
        {
            return IngredientCategory.DairyAndEggs;
        }

        if (lowerName.Contains("pasta") || lowerName.Contains("rice") || lowerName.Contains("noodle") || lowerName.Contains("flour") || lowerName.Contains("bread"))
        {
            return IngredientCategory.PastaAndGrains;
        }

        if (lowerName.Contains("salt") || lowerName.Contains("pepper") || lowerName.Contains("oregano") || lowerName.Contains("basil") || lowerName.Contains("cumin"))
        {
            return IngredientCategory.SpicesAndHerbs;
        }

        if (lowerName.Contains("oil") || lowerName.Contains("vinegar"))
        {
            return IngredientCategory.OilsAndVinegars;
        }

        return IngredientCategory.Produce;
    }

    // Parses a unit string into the Unit enum.
    private static Unit ParseUnit(string unit)
    {
        return unit.ToLowerInvariant() switch
        {
            "g" => Unit.G,
            "kg" => Unit.Kg,
            "ml" => Unit.Ml,
            "l" => Unit.L,
            "pcs" => Unit.Pcs,
            _ => Unit.Pcs
        };
    }

    // Parses a dish type string into the DishType enum.
    private static DishType ParseDishType(string dishType)
    {
        return dishType switch
        {
            "Pasta" => DishType.Pasta,
            "RiceBowl" => DishType.RiceBowl,
            "Noodles" => DishType.Noodles,
            "SoupStew" => DishType.SoupStew,
            "Salad" => DishType.Salad,
            "WrapTaco" => DishType.WrapTaco,
            "PizzaPie" => DishType.PizzaPie,
            "CasseroleBake" => DishType.CasseroleBake,
            "SandwichBurger" => DishType.SandwichBurger,
            "ProteinVegPlate" => DishType.ProteinVegPlate,
            "BreakfastDinner" => DishType.BreakfastDinner,
            "SnackBoard" => DishType.SnackBoard,
            _ => DishType.Other
        };
    }

    // Maps the created dish to the response.
    private static Response MapDish(Dish dish, IReadOnlyDictionary<Guid, Ingredient> ingredientLookup)
    {
        var ingredients = dish.Ingredients
            .OrderBy(ingredient => ingredient.SortOrder ?? int.MaxValue)
            .Select((ingredient, index) =>
            {
                var ingredientName = ingredientLookup.TryGetValue(ingredient.IngredientId, out var ingredientEntity)
                    ? ingredientEntity.Name
                    : string.Empty;

                var label = BuildIngredientLabel(ingredient.Quantity, ingredient.Unit, ingredientName);
                var id = $"{ingredient.IngredientId:D}-{index + 1}";

                return new DishIngredientResponse(
                    id,
                    ingredient.IngredientId.ToString("D"),
                    ingredient.Quantity,
                    label);
            })
            .ToArray();

        return new Response(
            dish.Id.ToString("D"),
            dish.Name,
            dish.DishType.ToString(),
            dish.PrepTimeMinutes,
            dish.CookTimeMinutes,
            dish.Servings,
            dish.Instructions,
            dish.IsSeafood,
            dish.IsVegetarian,
            dish.IsVegan,
            dish.VibeTags.ToArray(),
            ingredients);
    }

    // Builds a human-readable ingredient label.
    private static string BuildIngredientLabel(double quantity, Unit unit, string name)
    {
        var quantityLabel = FormatQuantity(quantity);
        var unitLabel = FormatUnit(unit);
        var trimmedName = name?.Trim() ?? string.Empty;

        var label = string.Join(
            " ",
            new[] { quantityLabel, unitLabel, trimmedName }
                .Where(value => !string.IsNullOrWhiteSpace(value)));

        return label;
    }

    // Formats ingredient quantities for display.
    private static string FormatQuantity(double quantity)
    {
        if (quantity <= 0)
        {
            return string.Empty;
        }

        var hasFraction = Math.Abs(quantity % 1) > 0.0001;
        var format = hasFraction ? "0.##" : "0";

        return quantity.ToString(format, CultureInfo.InvariantCulture);
    }

    // Formats unit values for labels.
    private static string FormatUnit(Unit unit) =>
        unit switch
        {
            Unit.G => "g",
            Unit.Kg => "kg",
            Unit.Ml => "ml",
            Unit.L => "l",
            Unit.Pcs => "pcs",
            _ => string.Empty
        };

    // Normalizes names for case-insensitive comparisons.
    private static string NormalizeName(string value) => value.Trim().ToUpperInvariant();

    private sealed record OpenAiChatCompletionResponse(
        [property: JsonPropertyName("choices")] IReadOnlyList<OpenAiChoice>? Choices);

    private sealed record OpenAiChoice(
        [property: JsonPropertyName("message")] OpenAiMessage? Message);

    private sealed record OpenAiMessage(
        [property: JsonPropertyName("content")] string? Content);

    private sealed record ClaudeMessagesResponse(
        [property: JsonPropertyName("content")] IReadOnlyList<ClaudeContentBlock>? Content);

    private sealed record ClaudeContentBlock(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("text")] string? Text);
}

internal enum SaveOutcome
{
    Success,
    Invalid,
    Conflict,
    Unavailable
}

internal sealed record UseCaseResult(
    SaveOutcome Outcome,
    Response? Dish,
    IReadOnlyList<ValidationError> Errors);

internal sealed record RecipeExpansionResult(
    bool IsSuccess,
    ExpandedRecipe? Recipe,
    string? ErrorMessage)
{
    public static RecipeExpansionResult Success(ExpandedRecipe recipe) =>
        new(true, recipe, null);

    public static RecipeExpansionResult Failure(string errorMessage) =>
        new(false, null, errorMessage);
}

internal sealed record ExpandedRecipe(
    string DishType,
    int PrepTimeMinutes,
    int CookTimeMinutes,
    int Servings,
    bool IsSeafood,
    bool IsVegetarian,
    bool IsVegan,
    IReadOnlyList<string> VibeTags,
    string? Instructions,
    IReadOnlyList<ExpandedIngredient> Ingredients);

internal sealed record ExpandedIngredient(string Name, double Quantity, string Unit);
