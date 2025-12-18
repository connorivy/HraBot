using System;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

namespace HraBot.Api.Features.Agents;

public static class CitationValidationBot
{
    public static AIAgent Create(IChatClient chatClient)
    {
        return chatClient.CreateAIAgent(
            name: AgentNames.CitationValidator,
            instructions: @"
You are a quality assurance assistant that validates the citations used in answers
provided by another AI assistant.

Your task is to ensure that 
1. if the answer contains information about health insurance and health reimbursement arrangements (HRAs),
   then there must be at least one citation provided.
2. Each citation must be relevant to the answer provided.

You will be provided with a json object in the following format:

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

Your response should be a json object in the following format:
{
  ""IsValid"": true|false,
  ""Issues"": [ ""string"" ]
}

Where: 
- IsValid is true if the citations are valid according to the criteria above, otherwise false.
- Issues is a list of strings describing any issues found with the citations. If there are no issues, this list should be empty.
"
        );
    }

    public static IServiceCollection AddCitationValidationBot(this IServiceCollection services)
    {
        services.AddAIAgent(
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

public record CitationValidationResponse(bool IsValid, List<string> Issues);
