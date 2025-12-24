using System.Text.Json;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace HraBot.Api.Features.Agents;

public class AgentLogger(ILogger<AgentLogger> logger)
{
    public async ValueTask<object?> FunctionLoggingMiddleware(
        Func<WorkflowEvent, CancellationToken, ValueTask> eventEmitter,
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation("Calling function named {name}", context.Function.Name);
        var result = await next(context, cancellationToken);
        if (result is JsonElement jsonElement)
        {
            var citations = JsonSerializer.Deserialize<List<Citation>>(jsonElement);
            if (citations is not null && citations.Count > 0)
            {
                await eventEmitter(new CitationsRetrievedEvent(citations), cancellationToken);
            }
        }
        logger.LogInformation(
            "Function named {name} responded with object of type {type} and serialized value {value}",
            context.Function.Name,
            result?.GetType(),
            JsonSerializer.Serialize(result)
        );

        return result;
    }
}
