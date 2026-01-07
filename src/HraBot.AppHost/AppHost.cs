using HraBot.AppHost;
using HraBot.ServiceDefaults;

var builder = DistributedApplication.CreateBuilder(args);

// You will need to set the connection string to your own value
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
//   dotnet user-secrets set ConnectionStrings:openai "Endpoint=https://models.inference.ai.azure.com;Key=YOUR-API-KEY"
// var openai = builder.AddConnectionString(HraServices.openai);

// You will need to set the connection string to your own value
//   dotnet user-secrets set ConnectionStrings:qdrantCloud "Endpoint=<qdrant-cloud-endpoint>:6334;Key=<qdrant-cloud-key>"
// Make sure to include the port number!!!
IResourceBuilder<IResourceWithConnectionString>? vectorDb = null;
if (!AppEnv.IsCiEnv)
{
    Console.WriteLine("Adding vectorDb connection string");
    vectorDb = builder.AddConnectionString(AppServices.vectorDb);
    Console.WriteLine("Added vectorDb connection string");
}

// var vectorDb = builder
//     .AddQdrant(HraServices.qdrantLocal)
//     .WithDataVolume()
//     .WithLifetime(ContainerLifetime.Persistent);

var postgres = builder.AddPostgres(AppServices.postgres).WithLifetime(ContainerLifetime.Persistent);
if (
    builder.Configuration["Headless"] is not string headlessValue
    || bool.TryParse(headlessValue, out var headless)
    || headless
)
{
    postgres = postgres.WithPgAdmin();
}

var db = postgres.AddDatabase(AppServices.hraBotDb);

var migrationService = builder
    .AddProject<Projects.HraBot_MigrationService>(AppServices.MIGRATION_SERVICE)
    .WithReference(db)
    .WaitFor(db)
    .ApplyTestEnvironmentOverrides();

// var markitdown = builder
//     .AddContainer(AppServices.MARK_IT_DOWN, "mcp/markitdown")
//     .WithArgs("--http", "--host", "0.0.0.0", "--port", "3001")
//     .WithHttpEndpoint(targetPort: 3001, name: "http");

var webApi = builder.AddProject<Projects.HraBot_Api>(AppServices.API);
webApi
    .WithReference(db)
    .WaitFor(db)
    .WithReference(migrationService)
    .WaitForCompletion(migrationService)
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
    // .WithEnvironment("MARKITDOWN_MCP_URL", markitdown.GetEndpoint("http"))
    .ApplyTestEnvironmentOverrides();
if (vectorDb is not null)
{
    webApi.WithReference(vectorDb).WaitFor(vectorDb);
}

var frontend = builder
    .AddViteApp(AppServices.WEB, "../hrabot-web")
    .WithEnvironment("VITE_API_ENDPOINT", webApi.GetEndpoint("https"))
    .WithReference(webApi)
    .WaitFor(webApi)
    .ApplyTestEnvironmentOverrides();

builder.Build().Run();
