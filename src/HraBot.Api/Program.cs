using HraBot.Api;
using HraBot.Api.Features.Agents;
using HraBot.Api.Features.Json;
using HraBot.Api.Features.Workflows;
using HraBot.Api.Services;
using HraBot.Api.Services.Ingestion;
using HraBot.Core;
using HraBot.ServiceDefaults;
using HraBot.Shared;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
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

builder.AddQdrantClient(HraServices.vectorDb);
builder.Services.RegisterAllServices(
    // #if !GENERATING_OPENAPI
    Environment.GetEnvironmentVariable($"ConnectionStrings__{HraServices.vectorDb}")
        ?? "Endpoint=dummy;Key=dummy",
    Environment.GetEnvironmentVariable($"ConnectionStrings__{HraServices.postgres}") ?? ""
// #endif
);
// builder
//     .AddWorkflow(WorkflowNames.Review, (sp, _) => ReturnApprovedResponse.CreateWorkflow(sp))
//     .AddAsAIAgent();

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
