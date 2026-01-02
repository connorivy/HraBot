using System.Text;
using System.Text.Json;
using HraBot.Api.Features.Json;
using HraBot.Core;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HraBot.Api.Features.Agents;

public static partial class HraBot
{
    public static AIAgent Create(IChatClient chatClient)
    {
        return chatClient
            .CreateAIAgent(
                name: AgentNames.HraBot,
                instructions: @"
You are an assistant who answers questions about health insurance, health reimbursement accounts, ICHRA, QSEHRA, and Take Command Health using provided retrieved snippets.

You will receive:
1) The running chat history
2) A system message that lists retrieved snippets with filenames and text.

Instructions:
- If the user asks about anything unrelated to the topics above, respond with Answer = ""I can only answer questions about health insurance"" and empty citations.
- Otherwise, rely only on the provided snippets; do not make up facts and do not search elsewhere.
- Set ""Question"" to the user's latest request (summarized if needed).
- Include up to 3 citations that directly support the answer. Keep each quote to 10 words or fewer and copy exact words from the snippet.

Return JSON with this shape:
{
  ""Question"": ""string"",
  ""Answer"": ""string"",
  ""Citations"": [
    {
      ""Filename"": ""string"",
      ""Quote"": ""string""
    }
  ]
}
"
            )
            .AsBuilder()
            .UseOpenTelemetry(AgentNames.HraBot
#if DEBUG
                , c => c.EnableSensitiveData = true
#endif
            )
            .Build();
    }

    public static IServiceCollection AddHraBotAgent(this IServiceCollection services)
    {
        services.AddAIAgent(
            AgentNames.HraBot,
            (sp, _) =>
            {
                var chatClient = sp.GetRequiredService<IChatClient>();
                return Create(chatClient);
            }
        );
        return services;
    }
}

public record HraBotResponse(string Question, string Answer, List<Citation> Citations);

public record Citation(string Filename, string Quote);

public sealed class HraBotExecutor(
    [FromKeyedServices(AgentNames.HraBot)] AIAgent hraBot,
    ILogger<HraBotExecutor> logger,
    AgentLogger agentLogger
) : Executor<RetrievedSearchContext, HraBotResponse>(AgentNames.HraBot + "Executor")
{
    public override async ValueTask<HraBotResponse> HandleAsync(
        RetrievedSearchContext answerContext,
        IWorkflowContext context,
        CancellationToken ct = default
    )
    {
        var hraBotWithMiddleware = hraBot
            .AsBuilder()
            .Use(
                (agent, funcContext, next, cancellationToken) =>
                    agentLogger.FunctionLoggingMiddleware(
                        context.AddEventAsync,
                        funcContext,
                        next,
                        cancellationToken
                    )
            )
            .Build();
        logger.LogInformation(
            "Retreiving response from HraBot with {citationCount} citations",
            answerContext.Citations.Count
        );
        var messages = BuildMessagesWithContext(answerContext);
        var response = await hraBotWithMiddleware.RunAsync(messages, cancellationToken: ct);
        logger.LogInformation("HraBot executor response: {response}", response);
        var structuredResponse =
            JsonSerializer.Deserialize(
                response.Text,
                HraBotJsonSerializerContext.Default.HraBotResponse
            )
            ?? throw new InvalidOperationException(
                $"Failed to parse HraBot response, {response.Text}."
            );
        return structuredResponse;
    }

    private static List<ChatMessage> BuildMessagesWithContext(RetrievedSearchContext context)
    {
        var messages = new List<ChatMessage>
        {
            new(Microsoft.Extensions.AI.ChatRole.System, BuildCitationContext(context.Citations)),
        };
        messages.AddRange(context.Messages);
        return messages;
    }

    private static string BuildCitationContext(IReadOnlyList<Citation> citations)
    {
        if (citations.Count == 0)
        {
            return "No retrieved context was available. If you cannot answer from past conversation, say you cannot find an answer in Take Command documents.";
        }

        var builder = new StringBuilder();
        builder.AppendLine(
            "Use only the following retrieved snippets to answer the user's question. Cite filenames and copy quotes verbatim (max 10 words)."
        );
        for (var i = 0; i < citations.Count; i++)
        {
            var citation = citations[i];
            builder.AppendLine($"[{i + 1}] {citation.Filename}: \"{citation.Quote}\"");
        }

        return builder.ToString();
    }
}

public class CitationsRetrievedEvent(List<Citation> citations) : WorkflowEvent(citations);
