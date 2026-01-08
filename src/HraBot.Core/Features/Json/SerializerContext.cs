using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.Serialization.SystemTextJson.Converters;
using HraBot.Api.Features.Agents;
using HraBot.Api.Features.Workflows;
using HraBot.Api.Services;
using HraBot.Core.Features.Chat;
using HraBot.Core.Features.Feedback;

namespace HraBot.Api.Features.Json;

[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = false
)]
[JsonSerializable(typeof(ChatRequest))]
[JsonSerializable(typeof(HraBotResponse))]
[JsonSerializable(typeof(List<Citation>))]
[JsonSerializable(typeof(CitationValidationResponse))]
[JsonSerializable(typeof(QueryRewriteResponse))]
[JsonSerializable(typeof(ChatRequestDto))]
[JsonSerializable(typeof(ApprovedResponse))]
[JsonSerializable(typeof(ApprovedResponseContract))]
[JsonSerializable(typeof(System.Net.ServerSentEvents.SseItem<string>))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(IReadOnlyList<IngestedChunkDto>))]
[JsonSerializable(typeof(EntityResponse<long>))]
[JsonSerializable(typeof(FeedbackContract))]
[JsonSerializable(typeof(List<FeedbackItemContract>))]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyRequest))]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyResponse))]
[JsonSerializable(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails))]
public partial class HraBotJsonSerializerContext : JsonSerializerContext
{
    public static JsonSerializerOptions DefaultOptions { get; }

    static HraBotJsonSerializerContext()
    {
        DefaultOptions = new() { PropertyNameCaseInsensitive = true };
        DefaultOptions.TypeInfoResolverChain.Insert(0, Default);
    }
}

public class CustomLambdaSerializer
    : SourceGeneratorLambdaJsonSerializer<HraBotJsonSerializerContext>
{
    protected override JsonSerializerOptions CreateDefaultJsonSerializationOptions()
    {
        return new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = new AwsNamingPolicy(JsonNamingPolicy.CamelCase),
            Converters =
            {
                (JsonConverter)new DateTimeConverter(),
                (JsonConverter)new MemoryStreamConverter(),
                (JsonConverter)new ConstantClassConverter(),
                (JsonConverter)new ByteArrayConverter(),
            },
        };
    }
}
