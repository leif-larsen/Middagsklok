var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.Middagsklok_Api>("api");
var frontend = builder.AddJavaScriptApp("frontend", "../frontend/middagsklok/")
    .WithExternalHttpEndpoints();

builder.Build().Run(); 