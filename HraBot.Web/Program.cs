using Microsoft.Extensions.AI;
using OpenAI;
using HraBot.Web.Components;
using HraBot.Web.Services;
using HraBot.Web.Services.Ingestion;
using HraBot.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// var openai = builder.AddAzureOpenAIClient("openai");
var openai = builder.AddAzureOpenAIClient(HraServices.openai);
openai.AddChatClient("gpt-4.1")
    .UseFunctionInvocation()
    .UseOpenTelemetry(configure: c =>
        c.EnableSensitiveData = builder.Environment.IsDevelopment());
openai.AddEmbeddingGenerator("text-embedding-3-small");

builder.AddQdrantClient("vectordb");
builder.Services.AddQdrantVectorStore();
builder.Services.AddQdrantCollection<Guid, IngestedChunk>(IngestedChunk.CollectionName);
builder.Services.AddSingleton<DataIngestor>();
builder.Services.AddSingleton<SemanticSearch>();
builder.Services.AddKeyedSingleton("ingestion_directory", new DirectoryInfo(Path.Combine(builder.Environment.WebRootPath, "Data")));

builder.Services.AddHttpClient("HraBot.Api", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["HraBotApiBaseUrl"] ?? throw new InvalidOperationException("HraBotApi:BaseUrl configuration is missing"));
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.UseStaticFiles();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
