using System.Text.Json;
using HraBot.Api.Features.Json;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

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
            var citations = JsonSerializer.Deserialize(
                jsonElement,
                HraBotJsonSerializerContext.Default.ListCitation
            );
            if (citations is not null && citations.Count > 0)
            {
                await eventEmitter(new CitationsRetrievedEvent(citations), cancellationToken);
            }
        }
        var serializedResult = result switch
        {
            null => "null",
            JsonElement element => element.GetRawText(),
            List<Citation> citations => JsonSerializer.Serialize(
                citations,
                HraBotJsonSerializerContext.Default.ListCitation
            ),
            HraBotResponse response => JsonSerializer.Serialize(
                response,
                HraBotJsonSerializerContext.Default.HraBotResponse
            ),
            CitationValidationResponse response => JsonSerializer.Serialize(
                response,
                HraBotJsonSerializerContext.Default.CitationValidationResponse
            ),
            _ => result.ToString() ?? string.Empty,
        };
        logger.LogInformation(
            "Function named {name} responded with object of type {type} and serialized value {value}",
            context.Function.Name,
            result?.GetType(),
            serializedResult
        );

        return result;
    }
}
