using System.Text.Json;
using HraBot.Api.Services;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace HraBot.Api.Features.Agents;

public static class HraBot
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
        return chatClient.CreateAIAgent(
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
            tools: [AIFunctionFactory.Create(semanticSearch.SearchAsync)]
        );
    }

    public static IServiceCollection AddHraBotAgent(this IServiceCollection services)
    {
        services.AddAIAgent(
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

public record HraBotResponse(string Question, string Answer, List<Citation> Citations);

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

sealed class StartExecutor() : Executor("ConcurrentStartExecutor")
{
    protected override Microsoft.Agents.AI.Workflows.RouteBuilder ConfigureRoutes(
        Microsoft.Agents.AI.Workflows.RouteBuilder routeBuilder
    )
    {
        return routeBuilder
            .AddHandler<List<ChatMessage>>(this.RouteMessages)
            .AddHandler<TurnToken>(this.RouteTurnTokenAsync);
    }

    private ValueTask RouteMessages(
        List<ChatMessage> messages,
        IWorkflowContext context,
        CancellationToken cancellationToken
    )
    {
        return context.SendMessageAsync(messages, cancellationToken: cancellationToken);
    }

    private ValueTask RouteTurnTokenAsync(
        TurnToken token,
        IWorkflowContext context,
        CancellationToken cancellationToken
    )
    {
        return context.SendMessageAsync(token, cancellationToken: cancellationToken);
    }
}

public sealed class CitationValidatorExecutor(
    [FromKeyedServices(AgentNames.CitationValidator)] AIAgent citationValidator
) : Executor<HraBotResponse, CitationValidationResponse>(AgentNames.CitationValidator + "Executor")
{
    public override async ValueTask<CitationValidationResponse> HandleAsync(
        HraBotResponse hraBotResponse,
        IWorkflowContext context,
        CancellationToken cancellationToken = default
    )
    {
        var result = await citationValidator.RunAsync(
            JsonSerializer.Serialize(hraBotResponse),
            cancellationToken: cancellationToken
        );
        var structuredResponse =
            JsonSerializer.Deserialize<CitationValidationResponse>(result.Text)
            ?? throw new InvalidOperationException(
                $"Failed to parse CitationValidator response, {result.Text}."
            );
        return structuredResponse;
    }
}
