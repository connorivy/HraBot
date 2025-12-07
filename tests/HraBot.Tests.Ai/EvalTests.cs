using System;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using Aspire.Hosting.Testing;
using Azure.AI.OpenAI;
using Azure.Identity;
using HraBot.Api;
using HraBot.Shared;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using TUnit.Core.Interfaces;

namespace HraBot.Tests.Ai;

[ParallelLimiter<AiParallelLimiter>]
public class EvalTests
{
    [Test]
    [MethodDataSource(
        typeof(IchraQuestionProvider), 
        nameof(IchraQuestionProvider.GetEvalQuestions)
    )]
    public async Task IchraQuestion_ShouldReturnExpectedAnswers(EvalQuestion evalQuestion)
    {
        var stream = SetupTestsAi.ApiClient.StreamChatResponse(new ChatRequestDto()
        {
            Messages = [
                new()
                {
                    Role = HraBot.Api.ChatRole.User,
                    Text = evalQuestion.Question
                }]
        }, CancellationToken.None);

        var fullResponse = new StringBuilder();
        await foreach (var message in stream)
        {
            fullResponse.AppendLine(message);
        }

        var x = DiskBasedReportingConfiguration.Create():

        ScenarioRun y = await x.CreateScenarioRunAsync();

        y.EvaluateAsync
    }

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
                    storageRootPath: Settings.Current.StorageRootPath,
                    chatConfiguration: chatConfig,
                    evaluators: [
                        rtcEvaluator,
                        coherenceEvaluator,
                        fluencyEvaluator,
                        groundednessEvaluator,
                        answerScoringEvaluator],
                    executionName: ExecutionName);
        }
}

public record EvalQuestion(int Id, string Question, string ExpectedAnswer);

public class AiParallelLimiter : IParallelLimit
{
    public int Limit => 2;
}