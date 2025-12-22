using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace HraBot.Api.Features.Agents;

public class AgentLogger(ILogger<AgentLogger> logger)
{
    // public async Task<AgentRunResponse> CustomMiddleware(
    //     IEnumerable<ChatMessage> messages,
    //     AgentThread? agentThread,
    //     AgentRunOptions? options,
    //     AIAgent innerAgent,
    //     CancellationToken ct
    // )
    // {
    //     logger.LogInformation("Running Agent with {numMessage} messages", messages.Count());
    //     var response = await innerAgent
    //         .RunAsync(messages, agentThread, options, ct)
    //         .ConfigureAwait(false);
    //     logger.LogInformation("Agent Res")
    //     Console.WriteLine(response.Messages.Count);
    //     return response;
    // }

    public static event EventHandler<List<Citation>>? CitationsRetrieved;

    public async ValueTask<object?> FunctionLoggingMiddleware(
        AIAgent agent,
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
                CitationsRetrieved?.Invoke(null, citations);
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
