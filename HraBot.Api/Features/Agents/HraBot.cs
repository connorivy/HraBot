using System.Text.Json;
using HraBot.Api.Services;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace HraBot.Api.Features.Agents;

public static class HraBot
{
    public static AIAgent Create(IChatClient chatClient, SemanticSearch semanticSearch)
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
",
            tools: [AIFunctionFactory.Create(semanticSearch.SearchAsync)]
        );
    }

    public static IServiceCollection AddHraBotAgent(this IServiceCollection services)
    {
        services.AddKeyedScoped<AIAgent>(
            AgentNames.HraBot,
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

public record HraBotResponse(string Answer, List<Citation> Citations);

public record Citation(string Filename, string Quote);

public sealed class HraBotExecutor([FromKeyedServices(AgentNames.HraBot)] AIAgent hraBot)
    : Executor<List<ChatMessage>, HraBotResponse>(AgentNames.HraBot + "Executor")
{
    public override async ValueTask<HraBotResponse> HandleAsync(
        List<ChatMessage> messages,
        IWorkflowContext context,
        CancellationToken cancellationToken = default
    )
    {
        var response = await hraBot.RunAsync(
            messages.Where(m => m.Role != Microsoft.Extensions.AI.ChatRole.System).ToList(),
            cancellationToken: cancellationToken
        );
        var structuredResponse =
            JsonSerializer.Deserialize<HraBotResponse>(response.Text)
            ?? throw new InvalidOperationException(
                $"Failed to parse HraBot response, {response.Text}."
            );
        return structuredResponse;
    }
}

public sealed class CitationValidatorExecutor(
    [FromKeyedServices(AgentNames.CitationValidator)] AIAgent citationValidator
) : Executor<HraBotResponse, bool>(AgentNames.CitationValidator + "Executor")
{
    public override async ValueTask<bool> HandleAsync(
        HraBotResponse response,
        IWorkflowContext context,
        CancellationToken cancellationToken = default
    )
    {
        var result = await citationValidator.RunAsync(
            response.ToString(),
            cancellationToken: cancellationToken
        );
        // return structuredResponse.Citations.All(c => !string.IsNullOrEmpty(c.Quote));
        return true;
    }
}
