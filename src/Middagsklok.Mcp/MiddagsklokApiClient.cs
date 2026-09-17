using System.Net;
using System.Net.Http.Json;

namespace Middagsklok.Mcp;

// Thin HTTP client over the Middagsklok API. Holds no state beyond the HttpClient it is given.
public sealed class MiddagsklokApiClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    // Returns the planning settings as raw JSON.
    public Task<string> GetPlanningSettings(CancellationToken cancellationToken) =>
        GetRequired("planning-settings", cancellationToken);

    // Returns the weekly plan for the start date as raw JSON, or null when no plan exists.
    public Task<string?> GetWeeklyPlan(string startDate, CancellationToken cancellationToken) =>
        GetOptional($"weekly-plans/{startDate}", cancellationToken);

    // Returns the dish repertoire as raw JSON.
    public Task<string> GetDishes(bool includeRetired, CancellationToken cancellationToken) =>
        GetRequired($"dishes?includeRetired={(includeRetired ? "true" : "false")}", cancellationToken);

    // Returns the scaled shopping list for the week as raw JSON, or null when no plan exists.
    public Task<string?> GetShoppingList(string startDate, CancellationToken cancellationToken) =>
        GetOptional($"shopping-list/{startDate}", cancellationToken);

    // Generates a plan for the week. The API persists it immediately.
    public async Task<string> GenerateWeeklyPlan(string startDate, CancellationToken cancellationToken)
    {
        var route = $"weekly-plans/generate/{startDate}";
        using var response = await _httpClient.PostAsync(route, null, cancellationToken);
        var body = await ReadBody(response, route, cancellationToken);

        return body;
    }

    // Replaces the plan for the week with the given days.
    public async Task<string> UpsertWeeklyPlan(string startDate, object request, CancellationToken cancellationToken)
    {
        var route = $"weekly-plans/{startDate}";
        using var response = await _httpClient.PutAsJsonAsync(route, request, cancellationToken);
        var body = await ReadBody(response, route, cancellationToken);

        return body;
    }

    // Sets or replaces the Oda mapping for an ingredient.
    public async Task<string> SetOdaMapping(string ingredientId, object request, CancellationToken cancellationToken)
    {
        var route = $"ingredients/{ingredientId}/oda-mapping";
        using var response = await _httpClient.PutAsJsonAsync(route, request, cancellationToken);
        var body = await ReadBody(response, route, cancellationToken);

        return body;
    }

    // Marks the week as eaten so its dishes count in the rotation history.
    public async Task MarkWeekEaten(string startDate, CancellationToken cancellationToken)
    {
        var route = $"weekly-plans/{startDate}/mark-eaten";
        using var response = await _httpClient.PostAsync(route, null, cancellationToken);
        await ReadBody(response, route, cancellationToken);
    }

    // Performs a GET that must succeed.
    private async Task<string> GetRequired(string route, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(route, cancellationToken);
        var body = await ReadBody(response, route, cancellationToken);

        return body;
    }

    // Performs a GET where 404 is a normal answer.
    private async Task<string?> GetOptional(string route, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(route, cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound)
        {
            return null;
        }

        var body = await ReadBody(response, route, cancellationToken);

        return body;
    }

    // Reads the body and turns any non-success status into a failure the tool layer can report.
    private static async Task<string> ReadBody(HttpResponseMessage response, string route, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new MiddagsklokApiFailure(response.StatusCode, route, body);
        }

        return body;
    }
}
