var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddAzurePostgresFlexibleServer("postgres")
    .RunAsContainer(c => {
        c.WithDataVolume("middagsklok-postgres-data");
        c.WithPgAdmin(pgAdmin =>
        {
            pgAdmin.WithHostPort(5050);
        });    
    });

var database = postgres.AddDatabase("middagsklok");

var apiService = builder.AddProject<Projects.Middagsklok_Api>("api", launchProfileName: "http")
    .WithReference(database)
    .WaitFor(database)
    .WithExternalHttpEndpoints();

var frontend = builder.AddJavaScriptApp("frontend", "../frontend/middagsklok/")
    .WithReference(apiService)
    .WithHttpEndpoint(port: 5900, env: "PORT")
    .WithExternalHttpEndpoints();

apiService.WithEnvironment("Cors__AllowedOrigins__0", frontend.GetEndpoint("http"));
    
builder.Build().Run();
