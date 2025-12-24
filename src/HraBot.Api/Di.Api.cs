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

    public static void AddAIAgentScoped(
        this IServiceCollection services,
        string name,
        Func<IServiceProvider, string, AIAgent> createAgentDelegate
    )
    {
        services.AddKeyedScoped(
            name,
            (sp, key) =>
            {
                var keyString =
                    key as string
                    ?? throw new InvalidOperationException(
                        $"key was expected to be string, but instead was type {key.GetType()}"
                    );
                var agent =
                    createAgentDelegate(sp, keyString)
                    ?? throw new InvalidOperationException(
                        $"The agent factory did not return a valid {nameof(AIAgent)} instance for key '{keyString}'."
                    );
                if (!string.Equals(agent.Name, keyString, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"The agent factory returned an agent with name '{agent.Name}', but the expected name is '{keyString}'."
                    );
                }

                return agent;
            }
        );
    }

    public static IHostedWorkflowBuilder AddWorkflowScoped(
        this IHostApplicationBuilder builder,
        string name,
        Func<IServiceProvider, string, Workflow> createWorkflowDelegate
    )
    {
        builder.Services.AddKeyedScoped(
            name,
            (sp, key) =>
            {
                var keyString =
                    key as string
                    ?? throw new InvalidOperationException(
                        $"Key was expected to be string, but instead was {key.GetType()}"
                    );
                var workflow =
                    createWorkflowDelegate(sp, keyString)
                    ?? throw new InvalidOperationException(
                        $"The agent factory did not return a valid {nameof(Workflow)} instance for key '{keyString}'."
                    );
                if (!string.Equals(workflow.Name, keyString, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"The workflow factory returned workflow with name '{workflow.Name}', but the expected name is '{keyString}'."
                    );
                }

                return workflow;
            }
        );

        return new HostedWorkflowBuilder(name, builder);
    }

    public static void AddAsScopedAIAgent(this IHostedWorkflowBuilder builder, string? name)
    {
        var workflowName = builder.Name;
        var agentName = name ?? workflowName;

        builder.HostApplicationBuilder.Services.AddAIAgentScoped(
            agentName,
            (sp, key) => sp.GetRequiredKeyedService<Workflow>(workflowName).AsAgent(name: key)
        );
    }
}

internal sealed class HostedWorkflowBuilder : IHostedWorkflowBuilder
{
    public string Name { get; }
    public IHostApplicationBuilder HostApplicationBuilder { get; }

    public HostedWorkflowBuilder(string name, IHostApplicationBuilder hostApplicationBuilder)
    {
        this.Name = name;
        this.HostApplicationBuilder = hostApplicationBuilder;
    }
}
