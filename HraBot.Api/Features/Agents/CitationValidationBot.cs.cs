using System;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace HraBot.Api.Features.Agents;

public static class CitationValidationBot
{
    public static AIAgent Create(IChatClient chatClient)
    {
        return chatClient.CreateAIAgent(
            name: "HraBot",
            instructions: @"
You are an assistant who answers questions about health insurance.
Do not answer questions about anything else.
Use only simple markdown to format your responses.

Use the Search tool to find relevant information. 

You must reply in JSON format as follows:

{
  ""Answer"": ""string"",
  ""Citations"": [
    {
      ""Filename"": ""string"",
      ""Quote"": ""string""
    }
  ]
}

The quote must be max 10 words, taken word-for-word from the search result, and is the basis for why the citation is relevant.
Don't refer to the presence of citations; just emit the citations in the 
"
        );
    }

    public static IServiceCollection AddCitationValidationBot(this IServiceCollection services)
    {
        services.AddKeyedScoped<AIAgent>(
            AgentNames.CitationValidator,
            (sp, _) =>
            {
                var chatClient = sp.GetRequiredService<IChatClient>();
                return Create(chatClient);
            }
        );
        return services;
    }
}
