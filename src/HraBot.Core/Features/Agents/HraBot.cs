using System.Text.Json;
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
You are an assistant who answers questions about health insurance.
Do not answer questions about anything else.
Use only simple markdown to format your responses.

Use the SearchAsync tool to find relevant information. 

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

The quote must be max 10 words, taken word-for-word from the search result, and is the basis for why the citation is relevant.
Don't refer to the presence of citations; just emit the citations in the JSON response.
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

    public static IServiceCollection AddDumbHraBot(this IServiceCollection services)
    {
        return services.AddKeyedSingleton<AIAgent>(
            AgentNames.HraBot,
            new DumbAiAgent<HraBotResponse>(
                new(
                    "Dummy original question",
                    "This is a dummy hra bot anwser",
                    [new("dummy-filename", "dummy-quote")]
                )
            )
        );
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
