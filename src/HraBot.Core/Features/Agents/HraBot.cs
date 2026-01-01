using System.Text.Json;
using GenerativeAI;
using HraBot.Api.Features.Json;
using HraBot.Api.Features.Workflows;
using HraBot.Api.Services;
using HraBot.Core;
using HraBot.Core.Features.Agents;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HraBot.Api.Features.Agents;

public static partial class HraBot
{
    public static AIAgent Create(
        IChatClient chatClient,
        SemanticSearch semanticSearch,
        AgentLogger agentLogger
    )
    // public static AIAgent Create(IChatClient chatClient)
    {
        // JsonElement responseSchema = AIJsonUtilities.CreateJsonSchema(typeof(HraBotResponse));
        // ChatOptions chatOptions = new()
        // {
        //     ResponseFormat = ChatResponseFormat.ForJsonSchema(
        //         schema: responseSchema,
        //         schemaName: "HraBotResponse",
        //         schemaDescription: "Response from HraBot including the original question, the bot's answer to the question, and citations"
        //     ),
        // };
        return chatClient
            .CreateAIAgent(
                name: AgentNames.HraBot,
                instructions: @"
You are an assistant who answers questions about health insurance, health reimbursement accounts, ICHRA, QSEHRA, and Take Command Health.

IF the user asked you about something unrelated these topics
    THEN response = {""Question"": {{originalMessage}}, ""Answer"": ""I can only answer questions about health insurance"", ""Citations"": [] }
    RETURN

// orchestrate searching a vector database for the answer to the question
TRANSFORM the user's question into a statement that will produce a similar embedding as the answer to the user's question
THEN use the SearchAsync tool to find relevant citations. 
THEN generate an answer and include up to 3 relevant citation summaries that are the basis of your answer.

You must reply in JSON format as follows:
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

For each citation that you include in the response, the quote must be max 10 words, taken word-for-word from the search result, and should be the basis for why the citation is relevant to the generated answer.
",
                tools:
                [
                    AIFunctionFactory.Create(
                        semanticSearch.SearchAsync,
                        "SearchAsync",
                        "Searches a vector database for document fragments related to the searchText",
                        HraBotJsonSerializerContext.DefaultOptions
                    ),
                ]
            )
            .AsBuilder()
            // .Use(agentLogger.FunctionLoggingMiddleware)
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
                var agentLogger = sp.GetRequiredService<AgentLogger>();
                var semanticSearch = sp.GetRequiredService<SemanticSearch>();
                return Create(chatClient, semanticSearch, agentLogger);
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
) : Executor<List<ChatMessage>, HraBotResponse>(AgentNames.HraBot + "Executor")
{
    public override async ValueTask<HraBotResponse> HandleAsync(
        List<ChatMessage> messages,
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
        logger.LogInformation("Retreiving response from HraBot");
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
}

public class CitationsRetrievedEvent(List<Citation> citations) : WorkflowEvent(citations);
