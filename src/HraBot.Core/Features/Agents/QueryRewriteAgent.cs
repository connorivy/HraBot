using System.Text.Json;
using HraBot.Api.Features.Json;
using HraBot.Core;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HraBot.Api.Features.Agents;

public static class QueryRewriteAgent
{
    public static AIAgent Create(IChatClient chatClient)
    {
        return chatClient
            .CreateAIAgent(
                name: AgentNames.QueryRewriteBot,
                instructions: @"
You rewrite user questions into multiple search-friendly variants.

You will receive the full chat history (including any assistant replies). Use it to infer the user's current question.

Return JSON exactly in this shape:
{
  ""originalQuestion"": ""summarize the latest user request in one sentence (e.g. 'how to reset my password')"",
  ""answerOrientedQuestion"": ""a direct answer-seeking rewrite (e.g. 'steps to reset user password')"",
  ""keywordOrientedQuestion"": ""a compact keyword phrase (e.g. 'password reset account')""
}
"
            )
            .AsBuilder()
            .UseOpenTelemetry(AgentNames.QueryRewriteBot
#if DEBUG
                , c => c.EnableSensitiveData = true
#endif
            )
            .Build();
    }

    public static IServiceCollection AddQueryRewriteAgent(this IServiceCollection services)
    {
        services.AddAIAgent(
            AgentNames.QueryRewriteBot,
            (sp, _) =>
            {
                var chatClient = sp.GetRequiredService<IChatClient>();
                return Create(chatClient);
            }
        );
        return services;
    }
}

public record QueryRewriteResponse(
    string OriginalQuestion,
    string AnswerOrientedQuestion,
    string KeywordOrientedQuestion
);

public record QueryRewriteResult(List<ChatMessage> Messages, QueryRewriteResponse Queries);

public sealed class QueryRewriteExecutor(
    [FromKeyedServices(AgentNames.QueryRewriteBot)] AIAgent queryRewriteAgent,
    ILogger<QueryRewriteExecutor> logger
) : Executor<List<ChatMessage>, QueryRewriteResult>(AgentNames.QueryRewriteBot + "Executor")
{
    public override async ValueTask<QueryRewriteResult> HandleAsync(
        List<ChatMessage> messages,
        IWorkflowContext context,
        CancellationToken ct = default
    )
    {
        logger.LogInformation("Retrieving query rewrites");
        var response = await queryRewriteAgent.RunAsync(messages, cancellationToken: ct);
        logger.LogInformation("QueryRewriteExecutor response: {response}", response);

        var structuredResponse =
            JsonSerializer.Deserialize(
                response.Text,
                HraBotJsonSerializerContext.Default.QueryRewriteResponse
            )
            ?? throw new InvalidOperationException(
                $"Failed to parse QueryRewrite response, {response.Text}."
            );

        return new QueryRewriteResult(messages, structuredResponse);
    }
}
