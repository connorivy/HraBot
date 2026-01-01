using System.Text.Json;
using System.Text.Json.Serialization;
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
[JsonSerializable(typeof(HraBotResponse))]
[JsonSerializable(typeof(List<Citation>))]
[JsonSerializable(typeof(CitationValidationResponse))]
[JsonSerializable(typeof(ChatRequestDto))]
[JsonSerializable(typeof(ApprovedResponse))]
[JsonSerializable(typeof(ApprovedResponseContract))]
[JsonSerializable(typeof(System.Net.ServerSentEvents.SseItem<string>))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(IReadOnlyList<IngestedChunkDto>))]
[JsonSerializable(typeof(EntityResponse<long>))]
[JsonSerializable(typeof(FeedbackContract))]
public partial class HraBotJsonSerializerContext : JsonSerializerContext
{
    public static JsonSerializerOptions DefaultOptions { get; }

    static HraBotJsonSerializerContext()
    {
        DefaultOptions = new() { PropertyNameCaseInsensitive = true };
        DefaultOptions.TypeInfoResolverChain.Insert(0, Default);
    }
}
