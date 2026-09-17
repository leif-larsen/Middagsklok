using System.Net;
using System.Text.Json;
using Middagsklok.Mcp;
using Middagsklok.Mcp.Tools;
using TUnit.Assertions;
using TUnit.Core;

namespace Middagsklok.Mcp.Tests.Tools;

public sealed class PlanningToolsTests
{
    private const string PlannedWeek = """
        {"id":"p1","startDate":"2026-09-18","isMarkedAsEaten":false,
         "days":[{"date":"2026-09-18","selection":{"type":"DISH","dishId":"d1"},"servings":null}]}
        """;

    private const string Dishes = """
        {"dishes":[{"id":"d1","name":"Taco","prepMinutes":15,"cookMinutes":35,"instructions":"x","retiredAt":null}]}
        """;

    // Verifies that generation is refused for a week that already has dishes, and the existing plan is returned.
    [Test]
    public async Task GenerateRefusesToOverwritePlannedWeek()
    {
        var handler = new StubHandler();
        handler.On(HttpMethod.Get, "weekly-plans/2026-09-18", HttpStatusCode.OK, PlannedWeek);
        var client = new MiddagsklokApiClient(handler.CreateClient());

        var result = await PlanningTools.GenerateWeeklyPlan(client, "2026-09-18");

        using var document = JsonDocument.Parse(result);
        await Assert.That(document.RootElement.TryGetProperty("warning", out _)).IsTrue();
        await Assert.That(document.RootElement.GetProperty("existingPlan").GetProperty("id").GetString()).IsEqualTo("p1");
        await Assert.That(handler.Calls.Any(call => call.Method == HttpMethod.Post)).IsFalse();
    }

    // Verifies that overwrite = true bypasses the guard and posts to the generator.
    [Test]
    public async Task GenerateOverwritesWhenAsked()
    {
        var handler = new StubHandler();
        handler.On(HttpMethod.Get, "weekly-plans/2026-09-18", HttpStatusCode.OK, PlannedWeek);
        handler.On(HttpMethod.Post, "weekly-plans/generate/2026-09-18", HttpStatusCode.OK, """{"id":"p2"}""");
        var client = new MiddagsklokApiClient(handler.CreateClient());

        var result = await PlanningTools.GenerateWeeklyPlan(client, "2026-09-18", overwrite: true);

        await Assert.That(result).Contains("p2");
    }

    // Verifies that a week without a plan generates without asking.
    [Test]
    public async Task GenerateRunsForMissingWeek()
    {
        var handler = new StubHandler();
        handler.On(HttpMethod.Get, "weekly-plans/2026-09-25", HttpStatusCode.NotFound, "{}");
        handler.On(HttpMethod.Post, "weekly-plans/generate/2026-09-25", HttpStatusCode.OK, """{"id":"p3"}""");
        var client = new MiddagsklokApiClient(handler.CreateClient());

        var result = await PlanningTools.GenerateWeeklyPlan(client, "2026-09-25");

        await Assert.That(result).Contains("p3");
    }

    // Verifies that dish ids in the plan are resolved to names.
    [Test]
    public async Task GetWeeklyPlanResolvesDishNames()
    {
        var handler = new StubHandler();
        handler.On(HttpMethod.Get, "weekly-plans/2026-09-18", HttpStatusCode.OK, PlannedWeek);
        handler.On(HttpMethod.Get, "dishes?includeRetired=true", HttpStatusCode.OK, Dishes);
        var client = new MiddagsklokApiClient(handler.CreateClient());

        var result = await PlanningTools.GetWeeklyPlan(client, "2026-09-18");

        await Assert.That(result).Contains("\"dishName\":\"Taco\"");
    }

    // Verifies that list_dishes adds total time and drops the long instructions text.
    [Test]
    public async Task ListDishesSummarises()
    {
        var handler = new StubHandler();
        handler.On(HttpMethod.Get, "dishes?includeRetired=false", HttpStatusCode.OK, Dishes);
        var client = new MiddagsklokApiClient(handler.CreateClient());

        var result = await PlanningTools.ListDishes(client);

        await Assert.That(result).Contains("\"totalMinutes\":50");
        await Assert.That(result).DoesNotContain("instructions");
    }

    // Verifies that API failures surface with status and route instead of being swallowed.
    [Test]
    public async Task ApiFailureCarriesStatusAndRoute()
    {
        var handler = new StubHandler();
        handler.On(HttpMethod.Post, "weekly-plans/2026-09-18/mark-eaten", HttpStatusCode.Conflict, """{"message":"already"}""");
        var client = new MiddagsklokApiClient(handler.CreateClient());

        var act = async () => await PlanningTools.MarkWeekEaten(client, "2026-09-18");

        var failure = await Assert.That(act).Throws<MiddagsklokApiFailure>();
        await Assert.That(failure!.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
        await Assert.That(failure.Route).Contains("mark-eaten");
    }

    // Minimal HTTP stub keyed on method and relative route.
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Dictionary<(HttpMethod Method, string Route), (HttpStatusCode Status, string Body)> _responses = new();

        public List<(HttpMethod Method, string Route)> Calls { get; } = [];

        // Registers a canned response.
        public void On(HttpMethod method, string route, HttpStatusCode status, string body) =>
            _responses[(method, route)] = (status, body);

        // Creates a client whose base address mirrors production.
        public HttpClient CreateClient() => new(this) { BaseAddress = new Uri("http://test/api/") };

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var route = request.RequestUri!.PathAndQuery["/api/".Length..];
            Calls.Add((request.Method, route));

            if (!_responses.TryGetValue((request.Method, route), out var canned))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent($"no stub for {request.Method} {route}")
                });
            }

            var response = new HttpResponseMessage(canned.Status) { Content = new StringContent(canned.Body) };

            return Task.FromResult(response);
        }
    }
}
