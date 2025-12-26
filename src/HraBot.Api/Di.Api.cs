using System;
using HraBot.Api.Features.Agents;
using HraBot.Api.Features.Workflows;
using HraBot.Shared;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;

namespace HraBot.Api;

public static class Di_Api
{
    public static void RegisterAiServices(this WebApplicationBuilder app)
    {
        app.Services.AddHraBotAgent();
        app.Services.AddCitationValidationBot();
        app.Services.AddSearchAgent();
        app.AddWorkflow(WorkflowNames.Review, (sp, _) => ReturnApprovedResponse.CreateWorkflow(sp))
            .AddAsAIAgent();
        app.Services.AddTransient<ReturnApprovedResponse>();
        app.Services.AddTransient<SearchBotExecutor>();
        app.Services.AddSingleton<HraBotExecutor>();
        app.Services.AddSingleton<CitationValidatorExecutor>();
        app.Services.AddSingleton<AiServiceProvider>();
        app.Services.AddSingleton<AiConfigInfoProvider>();
        // app.Services.AddScoped<ThreadProvider>();
        app.Services.AddSingleton<AgentLogger>();
    }
}
