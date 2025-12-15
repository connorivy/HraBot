using System;
using HraBot.Api.Features.Agents;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace HraBot.Api.Features.Workflows;

public class ReturnApprovedResponse(
    HraBotExecutor hraBot,
    CitationValidatorExecutor citationValidator
// [FromKeyedServices(AgentNames.HraBot)] AIAgent hraBot,
// [FromKeyedServices(AgentNames.CitationValidator)] AIAgent citationValidator
)
{
    public async Task<string?> GetApprovedResponse(
        IEnumerable<ChatMessage> messages,
        CancellationToken ct
    )
    {
        var workflow = new WorkflowBuilder(hraBot).AddEdge(hraBot, citationValidator).Build();

        await using StreamingRun run = await InProcessExecution.StreamAsync(
            workflow,
            messages.Where(m => m.Role != Microsoft.Extensions.AI.ChatRole.System).ToList(),
            cancellationToken: ct
        );
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
        await foreach (WorkflowEvent evt in run.WatchStreamAsync(ct))
        {
            Console.WriteLine($"{evt}");
            if (evt is WorkflowOutputEvent outputEvent)
            {
                return outputEvent.As<HraBotResponse>()?.Answer;
            }
        }
        return null;
    }
}
