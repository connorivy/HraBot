using HraBot.Api.Services;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace HraBot.Api.Features.Agents;

public record RetrievedSearchContext(List<ChatMessage> Messages, List<Citation> Citations);

public sealed class MultiQuerySearchExecutor(
    SemanticSearch semanticSearch,
    ILogger<MultiQuerySearchExecutor> logger
) : Executor<QueryRewriteResult, RetrievedSearchContext>(AgentNames.MultiQuerySearchExecutor)
{
    public override async ValueTask<RetrievedSearchContext> HandleAsync(
        QueryRewriteResult queryRewriteResult,
        IWorkflowContext context,
        CancellationToken ct = default
    )
    {
        logger.LogInformation("Running multi-query search");

        var originalTask = semanticSearch.SearchAsync(queryRewriteResult.Queries.OriginalQuestion);
        var answerOrientedTask = semanticSearch.SearchAsync(
            queryRewriteResult.Queries.AnswerOrientedQuestion
        );
        var keywordTask = semanticSearch.SearchAsync(
            queryRewriteResult.Queries.KeywordOrientedQuestion
        );

        await Task.WhenAll(originalTask, answerOrientedTask, keywordTask);

        var mergedCitations = MergeAndRerank(
            originalTask.Result,
            answerOrientedTask.Result,
            keywordTask.Result
        );

        await context.AddEventAsync(new CitationsRetrievedEvent(mergedCitations));
        logger.LogInformation(
            "Multi-query search produced {count} merged citations",
            mergedCitations.Count
        );

        return new RetrievedSearchContext(queryRewriteResult.Messages, mergedCitations);
    }

    private static List<Citation> MergeAndRerank(
        IReadOnlyList<IngestedChunkDto> original,
        IReadOnlyList<IngestedChunkDto> answerOriented,
        IReadOnlyList<IngestedChunkDto> keywordOriented
    )
    {
        var grouped = new[]
        {
            (Items: original, QueryIndex: 0),
            (Items: answerOriented, QueryIndex: 1),
            (Items: keywordOriented, QueryIndex: 2),
        };

        var scores = new Dictionary<(string Filename, string Quote), (int Hits, int QueryIndex, int Rank)>();

        foreach (var group in grouped)
        {
            for (var i = 0; i < group.Items.Count; i++)
            {
                var item = group.Items[i];
                var key = (item.Filename, item.Quote);
                if (scores.TryGetValue(key, out var score))
                {
                    score.Hits++;
                    score.QueryIndex = Math.Min(score.QueryIndex, group.QueryIndex);
                    score.Rank = Math.Min(score.Rank, i);
                    scores[key] = score;
                }
                else
                {
                    scores[key] = (1, group.QueryIndex, i);
                }
            }
        }

        return scores
            .OrderByDescending(s => s.Value.Hits)
            .ThenBy(s => s.Value.QueryIndex)
            .ThenBy(s => s.Value.Rank)
            .Select(s => new Citation(s.Key.Filename, s.Key.Quote))
            .Take(12)
            .ToList();
    }
}
