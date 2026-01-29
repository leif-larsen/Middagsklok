var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddAzurePostgresFlexibleServer("postgres")
    .RunAsContainer(c => c.WithDataVolume("middagsklok-postgres-data"));

var database = postgres.AddDatabase("middagsklok");

var apiService = builder.AddProject<Projects.Middagsklok_Api>("api")
    .WithReference(database)
    .WaitFor(database);

var frontend = builder.AddJavaScriptApp("frontend", "../frontend/middagsklok/")
    .WithHttpEndpoint(targetPort: 3000, env: "PORT")
    .WithExternalHttpEndpoints()
    .WithEnvironment("NEXT_PUBLIC_API_URL", apiService.GetEndpoint("http"));
    
builder.Build().Run();
