using HraBot.Api;
using HraBot.Api.Features.Json;
using HraBot.Api.Services;
using HraBot.Api.Services.Ingestion;
using HraBot.ServiceDefaults;
using HraBot.Shared;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Extensions.AI;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.AddServiceDefaults();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, HraBotJsonSerializerContext.Default);
});

builder
    .Services.AddChatClient(sp =>
    {
        ;
        return sp.GetRequiredService<AiServiceProvider>().GetRandomChatClient();
    })
    .UseFunctionInvocation()
    .UseOpenTelemetry(configure: c => c.EnableSensitiveData = builder.Environment.IsDevelopment());

builder.Services.AddEmbeddingGenerator(sp =>
    sp.GetRequiredService<AiServiceProvider>().GetRandomEmbeddingGeneratorClient()
);

builder.AddQdrantClient(HraServices.vectorDb);
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
builder.Services.AddQdrantVectorStore();
builder.Services.AddQdrantCollection<Guid, IngestedChunk>(IngestedChunk.CollectionName);
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
builder.Services.AddSingleton<DataIngestor>();
builder.Services.AddSingleton<SemanticSearch>();
builder.Services.AddKeyedSingleton(
    "ingestion_directory",
    new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "Data"))
);
builder.RegisterAiServices();

#if !GENERATING_OPENAPI
builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();
#endif

#if DEBUG
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});
#endif

var app = builder.Build();

#if DEBUG
app.UseCors();
#endif

// Register SSE Chat Endpoint
app.MapSseChatEndpoint();

#if !GENERATING_OPENAPI
app.MapOpenAIResponses();
app.MapOpenAIConversations();
#endif

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
#if !GENERATING_OPENAPI
    app.MapDevUI();
#endif
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapDefaultEndpoints();

// var sp = app.Services.CreateScope().ServiceProvider;
// var semanticSearch = sp.GetRequiredService<SemanticSearch>();
// await semanticSearch.LoadDocumentsAsync();

app.Run();
