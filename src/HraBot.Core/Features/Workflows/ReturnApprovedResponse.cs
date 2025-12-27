using HraBot.Api.Features.Agents;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HraBot.Api.Features.Workflows;

public class ReturnApprovedResponse(
    [FromKeyedServices(WorkflowNames.Review)] Workflow reviewWorkflow,
    ILogger<ReturnApprovedResponse> logger
)
{
    public async Task<ApprovedResponse> GetApprovedResponse(
        IEnumerable<ChatMessage> messages,
        CancellationToken ct
    )
    {
        await using StreamingRun run = await InProcessExecution.StreamAsync(
            reviewWorkflow,
            messages.Where(m => m.Role != Microsoft.Extensions.AI.ChatRole.System).ToList(),
            cancellationToken: ct
        );
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
        HraBotResponse? finalResponse = null;
        List<CitationValidationResponse> citationValidations = [];
        List<Citation>? citations = null;

        await foreach (WorkflowEvent evt in run.WatchStreamAsync(ct))
        {
            logger.LogInformation(
                "Receive workflow event of type {eventType}\nData payload of type {dataType}\nValue = {value}",
                evt.GetType().Name,
                evt.Data?.GetType().Name,
                evt.ToString()
            );
            if (evt is ExecutorFailedEvent failedEvt)
            {
                throw new InvalidOperationException(
                    $"Executor {failedEvt.ExecutorId} failed with error {failedEvt.Data}"
                );
            }
            if (evt.Data is HraBotResponse hraBotResponse)
            {
                finalResponse = hraBotResponse;
            }
            if (evt.Data is CitationValidationResponse citationValidation)
            {
                citationValidations.Add(citationValidation);
            }
            if (evt.Data is List<Citation> citationsData)
            {
                citations = citationsData;
            }
        }

        if (finalResponse is null)
        {
            throw new InvalidOperationException("HraBot did not produce a valid response.");
        }
        if (citationValidations.Count == 0)
        {
            throw new InvalidOperationException("No citation validations were created");
        }

        if (citationValidations.All(cv => cv.IsValid))
        {
            return new(ResponseType.Success, finalResponse.Answer, finalResponse.Citations);
        }
        return new(
            ResponseType.Failure,
            "Unable to find an answer in the company documents",
            citations ?? []
        );
    }

    public static Workflow CreateWorkflow(IServiceProvider sp)
    {
        var startExecutor = new StartExecutor();
        // var searchBotExecutor = sp.GetRequiredService<SearchBotExecutor>();
        var hraBotExecutor = sp.GetRequiredService<HraBotExecutor>();
        var citationValidatorExecutor = sp.GetRequiredService<CitationValidatorExecutor>();
        var workflowBuilder = new WorkflowBuilder(startExecutor)
            // .AddEdge(startExecutor, searchBotExecutor)
            // .AddEdge(searchBotExecutor, hraBotExecutor)
            .AddEdge(startExecutor, hraBotExecutor)
            .AddEdge(hraBotExecutor, citationValidatorExecutor);
        workflowBuilder.WithName(WorkflowNames.Review);
        return workflowBuilder.Build();
    }
}

public record ApprovedResponse(
    ResponseType ResponseType,
    string Response,
    List<Citation> Citations
);

public enum ResponseType
{
    Undefined = 0,
    Success,
    Failure,
}
