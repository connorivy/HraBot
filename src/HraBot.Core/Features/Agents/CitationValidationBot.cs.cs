using System.Text.Json;
using HraBot.Api.Features.Json;
using HraBot.Core;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HraBot.Api.Features.Agents;

public static class CitationValidationBot
{
    public static AIAgent Create(IChatClient chatClient)
    {
        JsonElement responseSchema = AIJsonUtilities.CreateJsonSchema(
            typeof(CitationValidationResponse),
            serializerOptions: HraBotJsonSerializerContext.DefaultOptions
        );
        ChatOptions chatOptions = new()
        {
            Instructions =
                @"
You are a quality assurance assistant that validates the citations used in answers
provided by another AI assistant.

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
 
You will evaluate the other agent's answer by following this logical flow

Does the provided ANSWER contain specific information about one of the following topics? Health insurance, Take Command Health, HRAs, Taxes specific to health expenses
(Make sure to distiguish between responses that contain words like 'health insurance' versus responses that actually contain information about health insurance)
IF (YES) {
    Is there at least one provided CITATION?
    IF (YES) {
        Is the CITATION relevant to the provided ANSWER?
        IF (YES) {
            Response is valid
            exit;
        } ELSE {
            Response is NOT valid
            exit;
        }
    } ELSE {
        Response is not valid
        exit;
    }
} ELSE {
    Response is valid
    exit;
}


Your response should be a json object in the following format:
{
  ""IsValid"": true|false,
  ""Issues"": [ ""string"" ]
}

Where: 
- IsValid is true if the citations are valid according to the criteria above, otherwise false.
- Issues is a list of strings describing any issues found with the citations. If there are no issues, this list should be empty.
",
            ResponseFormat = ChatResponseFormat.ForJsonSchema(
                schema: responseSchema,
                schemaName: "CitationValidationResponse",
                schemaDescription: @$"Response from {AgentNames.CitationValidator} which includes ""IsValid"" of type bool value and ""Issues"" of type string"
            ),
        };
        return chatClient.CreateAIAgent(
            new ChatClientAgentOptions()
            {
                Name = AgentNames.CitationValidator,
                ChatOptions = chatOptions,
            }
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

public sealed class CitationValidatorExecutor(
    [FromKeyedServices(AgentNames.CitationValidator)] AIAgent citationValidator,
    ILogger<CitationValidatorExecutor> logger
)
    : Executor<HraBotResponseWithRawJson, CitationValidationResponse>(
        AgentNames.CitationValidator + "Executor"
    )
{
    public override async ValueTask<CitationValidationResponse> HandleAsync(
        HraBotResponseWithRawJson hraBotResponse,
        IWorkflowContext context,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation("Retreiving response from CitationValidator");
        var response = await citationValidator.RunAsync(
            hraBotResponse.RawJson,
            cancellationToken: cancellationToken
        );
        logger.LogInformation("CitationValidatorExecutor response: {response}", response);
        var structuredResponse =
            JsonSerializer.Deserialize(
                response.Text,
                HraBotJsonSerializerContext.Default.CitationValidationResponse
            )
            ?? throw new InvalidOperationException(
                $"Failed to parse CitationValidator response, {response.Text}."
            );
        return structuredResponse;
    }
}
