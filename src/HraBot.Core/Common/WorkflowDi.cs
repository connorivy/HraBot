using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace HraBot.Core.Common;

public static class WorkflowDi
{
    public static void AddWorkflowAsAgent(
        this IServiceCollection services,
        string name,
        Func<IServiceProvider, string, Workflow> createWorkflowDelegate
    )
    {
        services.AddKeyedSingleton(
            name,
            (sp, key) =>
            {
                var keyString =
                    key as string
                    ?? throw new InvalidOperationException("Workflow key cannot be null");
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
        services.AddAIAgent(
            name,
            (sp, key) => sp.GetRequiredKeyedService<Workflow>(name).AsAgent(name: key)
        );
    }
}
