using System.Runtime.CompilerServices;
using System.Text.Json;
using HraBot.Api.Features.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace HraBot.Core.Features.Agents;

internal class DumbAiAgent<TResponse>(TResponse response) : AIAgent
{
    public override AgentThread DeserializeThread(
        JsonElement serializedThread,
        JsonSerializerOptions? jsonSerializerOptions = null
    )
    {
        throw new NotImplementedException();
    }

    public override AgentThread GetNewThread()
    {
        throw new NotImplementedException();
    }

    public override Task<AgentRunResponse> RunAsync(
        IEnumerable<ChatMessage> messages,
        AgentThread? thread = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult(
            new AgentRunResponse(
                new ChatMessage(
                    Microsoft.Extensions.AI.ChatRole.Assistant,
                    JsonSerializer.Serialize(
                        response,
                        typeof(TResponse),
                        HraBotJsonSerializerContext.Default
                    )
                )
            )
        );
    }

    public override IAsyncEnumerable<AgentRunResponseUpdate> RunStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentThread? thread = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return RunStreamingCoreAsync(messages, thread, options, cancellationToken);
    }

    private async IAsyncEnumerable<AgentRunResponseUpdate> RunStreamingCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentThread? thread,
        AgentRunOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var response = await RunAsync(messages, thread, options, cancellationToken)
            .ConfigureAwait(false);
        foreach (var update in response.ToAgentRunResponseUpdates())
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return update;
        }
    }
}
