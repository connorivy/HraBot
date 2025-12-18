using HraBot.Api.Features.Agents;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace HraBot.Api.Features.Workflows;

public class ReturnApprovedResponse(
    [FromKeyedServices(WorkflowNames.Review)] Workflow reviewWorkflow
)
{
    public async Task<string?> GetApprovedResponse(
        IEnumerable<ChatMessage> messages,
        CancellationToken ct
    )
    {
        await using StreamingRun run = await InProcessExecution.StreamAsync(
            (Workflow)reviewWorkflow,
            messages.Where(m => m.Role != Microsoft.Extensions.AI.ChatRole.System).ToList(),
            cancellationToken: ct
        );
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
        HraBotResponse? finalResponse = null;
        List<CitationValidationResponse> citationValidations = [];
        await foreach (WorkflowEvent evt in run.WatchStreamAsync(ct))
        {
            Console.WriteLine($"{evt}");
            Console.WriteLine($"evt type = {evt.GetType().Name}");
            if (evt.Data is HraBotResponse hraBotResponse)
            {
                finalResponse = hraBotResponse;
            }
            if (evt.Data is CitationValidationResponse citationValidation)
            {
                citationValidations.Add(citationValidation);
            }
        }

        if (finalResponse is null)
        {
            throw new InvalidOperationException("HraBot did not produce a valid response.");
        }
        Console.WriteLine($"Final Answer: {finalResponse.Answer}");
        foreach (var citation in finalResponse.Citations)
        {
            Console.WriteLine($"Citation: Filename={citation.Filename}, Quote={citation.Quote}");
        }

        foreach (var issue in citationValidations.SelectMany(cv => cv.Issues))
        {
            Console.WriteLine($"Citation issue: {issue}");
        }

        if (citationValidations.All(cv => cv.IsValid))
        {
            return finalResponse?.Answer;
        }
        return null;
    }
}
