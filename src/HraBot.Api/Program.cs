using System.Text.Json.Serialization;
using HraBot.Api;
using HraBot.Api.Features.Json;
using HraBot.Core;
using HraBot.ServiceDefaults;
using Microsoft.Agents.AI.DevUI;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(o =>
    o.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_1
);

builder.AddServiceDefaults();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, HraBotJsonSerializerContext.Default);
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    options.SerializerOptions.RespectNullableAnnotations = true;
});

builder.Services.RegisterAllServices(
    Environment.GetEnvironmentVariable($"ConnectionStrings__{AppServices.vectorDb}")
        ?? "Endpoint=dummy;Key=dummy",
    Environment.GetEnvironmentVariable($"ConnectionStrings__{AppServices.hraBotDb}") ?? ""
);

#if !GENERATING_OPENAPI
builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();
#endif

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();

app.MapEndpoints();

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
// if (
//     Environment.GetEnvironmentVariable(AppOptions.MockChatClient_bool)
//         is string mockChatClientString
//     && bool.TryParse(mockChatClientString, out var mockChatClient)
//     && mockChatClient
// )
// {
//     return services.AddDumbAiServices();
// }

app.Run();
