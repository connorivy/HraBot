using HraBot.Api.Services;
using Microsoft.Agents;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

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
                name: AgentNames.HraBot,
                instructions: @"
You are an AI agent who receives questions from a user and then searches for relevant resources.

Use the SearchAsync tool to find the relevant information. 

You must reply in JSON format as follows:

[
  {
    ""documentid"" : ""string"",
    ""text"" : ""string""
  }
]
",
                tools: [AIFunctionFactory.Create(semanticSearch.SearchAsync)]
            )
            .WithOpenTelemetry();
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
