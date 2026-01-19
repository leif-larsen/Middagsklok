var builder = DistributedApplication.CreateBuilder(args);

// Add the Next.js frontend as an external process
// Path is relative to the AppHost project directory
var frontend = builder.AddNpmApp("frontend", "../frontend", "dev")
    .WithHttpEndpoint(port: 3000, env: "PORT")
    .WithExternalHttpEndpoints();

// Add the Middagsklok API
var api = builder.AddProject<Projects.Middagsklok_Api>("api")
    .WithHttpEndpoint(port: 5000, name: "http")
    .WithEnvironment("AllowedOrigins__0", frontend.GetEndpoint("http"));

// Pass API URL to frontend
frontend.WithEnvironment("NEXT_PUBLIC_API_BASE_URL", api.GetEndpoint("http"));

builder.Build().Run();
