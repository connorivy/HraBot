using System.Text;
using HraBot.Shared;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using TUnit.Core.Interfaces;

namespace HraBot.Tests.Ai;

[ParallelLimiter<AiParallelLimiter>]
[Explicit]
public class EvalTests
{
    [Test]
    [MethodDataSource(
        typeof(IchraQuestionProvider),
        nameof(IchraQuestionProvider.GetEvalQuestions)
    )]
    public async Task IchraQuestion_ShouldReturnExpectedAnswers(EvalQuestion evalQuestion)
    {
        var stream = SetupTestsAi.ApiClient.StreamChatResponse(
            new Api.ChatRequestDto()
            {
                Messages =
                [
                    new() { Role = HraBot.Api.ChatRole.User, Text = evalQuestion.Question },
                ],
            },
            CancellationToken.None
        );

        var fullResponse = new StringBuilder();
        await foreach (var message in stream)
        {
            fullResponse.AppendLine(message);
        }

        var reportConfig = GetReportingConfiguration();
        await using var scenario = await reportConfig.CreateScenarioRunAsync(
            scenarioName: "IchraQuestionEval",
            iterationName: evalQuestion.Id.ToString()
        );

        var evaluationResults = await scenario.EvaluateAsync(
            [new ChatMessage(role: ChatRole.User, content: evalQuestion.Question)],
            new ChatResponse(
                new ChatMessage(role: ChatRole.Assistant, content: fullResponse.ToString())
            ),
            additionalContext: [new AnswerScoringEvaluator.Context(evalQuestion.ExpectedAnswer)]
        );

        if (
            evaluationResults.Metrics.Values.Any(m =>
                m.Interpretation?.Rating == EvaluationRating.Inconclusive
            )
        )
        {
            throw new InvalidOperationException("Evaluation was inconclusive");
        }
    }

    private static readonly string ExecutionName = $"{DateTime.UtcNow:yyyyMMddTHHmmss}";

    static ReportingConfiguration GetReportingConfiguration()
    {
        // Setup and configure the evaluators you would like to utilize for each AI chat
#pragma warning disable AIEVAL001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        IEvaluator rtcEvaluator = new RelevanceTruthAndCompletenessEvaluator();
#pragma warning restore AIEVAL001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        IEvaluator coherenceEvaluator = new CoherenceEvaluator();
        IEvaluator fluencyEvaluator = new FluencyEvaluator();
        IEvaluator groundednessEvaluator = new GroundednessEvaluator();
        IEvaluator answerScoringEvaluator = new AnswerScoringEvaluator();

        var chatClient = ChatClientUtils.CreateGithubModelsChatClient(
            SetupTestsAi.GetOpenAiApiKey(),
            "gpt-4o-mini"
        );
        var chatConfig = new ChatConfiguration(chatClient);

        return DiskBasedReportingConfiguration.Create(
            storageRootPath: Path.Combine(GetProjectRoot(), "AiEvalReports"),
            chatConfiguration: chatConfig,
            evaluators:
            [
                rtcEvaluator,
                coherenceEvaluator,
                fluencyEvaluator,
                groundednessEvaluator,
                answerScoringEvaluator,
            ],
            executionName: ExecutionName
        );
    }

    static string GetProjectRoot()
    {
        var dir = AppContext.BaseDirectory; // bin/Debug/net8.0/
        return Directory.GetParent(dir)!.Parent!.Parent!.FullName;
    }
}

public record EvalQuestion(int Id, string Question, string ExpectedAnswer);

public class AiParallelLimiter : IParallelLimit
{
    public int Limit => 2;
}

public class DummyTest
{
    [Test]
    public void Dummy() { }
}
