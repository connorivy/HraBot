using HraBot.ServiceDefaults;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

var builder = DistributedApplication.CreateBuilder(args);

// You will need to set the connection string to your own value
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
//   dotnet user-secrets set ConnectionStrings:openai "Endpoint=https://models.inference.ai.azure.com;Key=YOUR-API-KEY"
var openai = builder.AddConnectionString(HraServices.openai);

// You will need to set the connection string to your own value
//   dotnet user-secrets set ConnectionStrings:qdrantCloud "Endpoint=<qdrant-cloud-endpoint>;Key=<qdrant-cloud-key>"
var vectorDB = builder.AddConnectionString(HraServices.qdrantCloud);

// var vectorDB = builder
//     .AddQdrant(HraServices.qdrantLocal)
//     .WithDataVolume()
//     .WithLifetime(ContainerLifetime.Persistent);

var markitdown = builder
    .AddContainer("markitdown", "mcp/markitdown")
    .WithArgs("--http", "--host", "0.0.0.0", "--port", "3001")
    .WithHttpEndpoint(targetPort: 3001, name: "http");

var webApi = builder.AddProject<Projects.HraBot_Api>("api");
webApi
    .WithReference(openai)
    .WaitFor(openai)
    .WithReference(vectorDB)
    .WaitFor(vectorDB)
    .WithUrls(context =>
    {
        foreach (var u in context.Urls)
        {
            u.DisplayLocation = UrlDisplayLocation.DetailsOnly;
        }

        context.Urls.Add(
            new()
            {
                Url = "/scalar",
                DisplayText = "API Reference",
                Endpoint = context.GetEndpoint("https"),
            }
        );

        context.Urls.Add(
            new()
            {
                Url = "/devui",
                DisplayText = "Dev UI",
                Endpoint = context.GetEndpoint("https"),
            }
        );
    })
    .WithEnvironment("MARKITDOWN_MCP_URL", markitdown.GetEndpoint("http"));

var webApp = builder.AddProject<Projects.HraBot_Web>("aichatweb-app");
webApp
    .WithReference(openai)
    .WithReference(vectorDB)
    .WaitFor(vectorDB)
    .WithReference(webApi)
    .WaitFor(webApi)
    .WithEnvironment("MARKITDOWN_MCP_URL", markitdown.GetEndpoint("http"));

builder.Build().Run();
