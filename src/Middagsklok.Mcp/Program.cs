using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Middagsklok.Mcp;
using Middagsklok.Mcp.Tools;

var apiUrl = Environment.GetEnvironmentVariable("MIDDAGSKLOK_API_URL");

if (string.IsNullOrWhiteSpace(apiUrl))
{
    throw new ApiUrlMissing("Set MIDDAGSKLOK_API_URL to the API base, for example http://praxis-server:3000/api.");
}

var builder = Host.CreateApplicationBuilder(args);

// stdout carries the MCP protocol, so every log line has to go to stderr.
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddHttpClient<MiddagsklokApiClient>(client =>
{
    client.BaseAddress = new Uri(apiUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<PlanningTools>();

await builder.Build().RunAsync();
