using System.Text.Json;
using HraBot.Api.Features.Json;
using HraBot.Api.Services;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HraBot.Api.Features.Agents;

public static class SearchAgent
{
    public static AIAgent Create(IChatClient chatClient, SemanticSearch semanticSearch)
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
                name: AgentNames.SearchBot,
                instructions: @"
You are an AI agent who receives questions from a user and then searches for relevant resources.

Use the SearchAsync tool to find the relevant information. 

You must reply in JSON format as follows:

[
  {
    ""Filename"" : ""string"",
    ""Quote"" : ""string""
  }
]
",
                tools: [
                    AIFunctionFactory.Create(
                        semanticSearch.SearchAsync,
                        "SearchAsync",
                        "Searches a vector database for document fragments related to the searchText",
                        HraBotJsonSerializerContext.DefaultOptions
                    ),
                ]
            )
            .AsBuilder()
            .UseOpenTelemetry(AgentNames.SearchBot, 
#if DEBUG 
           c => c.EnableSensitiveData = true
#endif
             )
            .Build();
    }

    public static IServiceCollection AddSearchAgent(this IServiceCollection services)
    {
        services.AddAIAgent(
            AgentNames.SearchBot,
            (sp, _) =>
            {
                var chatClient = sp.GetRequiredService<IChatClient>();
                var semanticSearch = sp.GetRequiredService<SemanticSearch>();
                return Create(chatClient, semanticSearch);
            }
        );
        return services;
    }
}

public sealed class SearchBotExecutor(
    [FromKeyedServices(AgentNames.SearchBot)] AIAgent agent,
    ILogger<SearchBotExecutor> logger
) : Executor<List<ChatMessage>, HraBotState>(AgentNames.SearchBot + "Executor")
{
    public override async ValueTask<HraBotState> HandleAsync(
        List<ChatMessage> messages,
        IWorkflowContext context,
        CancellationToken ct = default
    )
    {
        logger.LogInformation("Retreiving response from SearchBot");
        var state = new HraBotState()
        {
            Messages = messages
        };
        var response = await agent.RunAsync(
            messages,
            cancellationToken: ct
        );
        state.Messages.AddRange(response.Messages);
        var threadId = Guid.NewGuid().ToString();
        await context.QueueStateUpdateAsync(threadId, messages, cancellationToken: ct);
        logger.LogInformation("SearchBotExecutor response: {response}", response);
        var structuredResponse =
            JsonSerializer.Deserialize(
                response.Text,
                HraBotJsonSerializerContext.Default.ListCitation
            )
            ?? throw new InvalidOperationException(
                $"Failed to parse SearchBot response, {response.Text}."
            );
        await context.AddEventAsync(new CitationsRetrievedEvent(structuredResponse));
        return state;
    }
}

public class HraBotState
{
    public required List<ChatMessage> Messages { get; init; }
}
