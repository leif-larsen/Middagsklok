var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.Middagsklok_Api>("api");
var nextJsApp = builder.AddJavaScriptApp("nextjs-app", "../Middagsklok.NextJsApp", "dev")
    .WithReference(apiService);

builder.Build().Run();
