using HraBot.Api;
using HraBot.Api.Services;
using HraBot.Api.Services.Ingestion;
using HraBot.ServiceDefaults;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Extensions.AI;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.AddServiceDefaults();

var openai = builder.AddAzureOpenAIClient(HraServices.openai);
openai
    .AddChatClient("gpt-4.1")
    .UseFunctionInvocation()
    .UseOpenTelemetry(configure: c => c.EnableSensitiveData = builder.Environment.IsDevelopment());
openai.AddEmbeddingGenerator("text-embedding-3-small");

builder.AddQdrantClient("vectordb");
builder.Services.AddQdrantVectorStore();
builder.Services.AddQdrantCollection<Guid, IngestedChunk>(IngestedChunk.CollectionName);
builder.Services.AddSingleton<DataIngestor>();
builder.Services.AddSingleton<SemanticSearch>();
builder.Services.AddKeyedSingleton(
    "ingestion_directory",
    new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "Data"))
);
builder.RegisterAiServices();
builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

var app = builder.Build();

// Register SSE Chat Endpoint
app.MapSseChatEndpoint();

app.MapOpenAIResponses();
app.MapOpenAIConversations();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapDevUI();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapDefaultEndpoints();

// var sp = app.Services.CreateScope().ServiceProvider;
// var semanticSearch = sp.GetRequiredService<SemanticSearch>();
// await semanticSearch.LoadDocumentsAsync();

app.Run();
