using System;
using HraBot.Api.Features.Agents;
using HraBot.Api.Features.Workflows;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;

namespace HraBot.Api;

public static class Di_Api
{
    public static void RegisterAiServices(this WebApplicationBuilder app)
    {
        app.Services.AddHraBotAgent();
        app.Services.AddCitationValidationBot();
        app.AddWorkflow(
                WorkflowNames.Review,
                (sp, _) =>
                {
                    var startExecutor = new StartExecutor();
                    var hraBotExecutor = sp.GetRequiredService<HraBotExecutor>();
                    var citationValidatorExecutor =
                        sp.GetRequiredService<CitationValidatorExecutor>();
                    var workflowBuilder = new WorkflowBuilder(startExecutor)
                        .AddEdge(startExecutor, hraBotExecutor)
                        .AddEdge(hraBotExecutor, citationValidatorExecutor);
                    workflowBuilder.WithName(WorkflowNames.Review);
                    return workflowBuilder.Build();
                }
            )
            .AddAsAIAgent();
        app.Services.AddTransient<ReturnApprovedResponse>();
        app.Services.AddTransient<HraBotExecutor>();
        app.Services.AddTransient<CitationValidatorExecutor>();
    }
}
