using HraBot.Api.Features.Agents;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HraBot.Api.Features.Workflows;

public class GetApprovedResponseWorkflow(
    [FromKeyedServices(WorkflowNames.Review)] Workflow reviewWorkflow,
    ILogger<GetApprovedResponseWorkflow> logger
)
{
    public virtual async Task<ApprovedResponse> GetApprovedResponse(
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
        string response;
        if (citations?.Count > 0)
        {
            response =
                "I was unable to confidently determine the answer. Here are some documents that may be relevent to your question.";
        }
        else
        {
            response =
                "I was unable to find an answer to your question in the Take Command documents";
        }
        return new(ResponseType.Failure, response, citations ?? []);
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

internal class GetDummyApprovedResponseWorkflow() : GetApprovedResponseWorkflow(null!, null!)
{
    public override Task<ApprovedResponse> GetApprovedResponse(
        IEnumerable<ChatMessage> messages,
        CancellationToken ct
    )
    {
        return Task.FromResult(
            new ApprovedResponse(
                ResponseType.Success,
                "This is a dummy response",
                [new("dummy-filename", "dummy-quote")]
            )
        );
    }
}
