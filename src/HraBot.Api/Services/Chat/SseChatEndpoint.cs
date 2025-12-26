using System.ClientModel;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HraBot.Api.Features.Workflows;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using OpenAI;

namespace HraBot.Api;

public static class SseChatEndpoint
{
    public static void MapSseChatEndpoint(this WebApplication app)
    {
        app.MapPost(
            "/api/chat/stream",
            (
                [FromServices] IChatClient chatClient,
                [FromBody] ChatRequestDto request,
                CancellationToken ct
            ) => StreamHraBotResponse(chatClient, request, ct)
        );
        app.MapPost(
            "/api/hrabot",
            async (
                [FromServices] ReturnApprovedResponse workflow,
                [FromBody] ChatRequestDto request,
                CancellationToken ct
            ) =>
                await workflow.GetApprovedResponse(
                    request.Messages.Select(m => new ChatMessage(
                        ChatRoleMapper.Map(m.Role),
                        m.Text
                    )),
                    ct
                )
        );
    }

    private static ServerSentEventsResult<string> StreamHraBotResponse(
        IChatClient chatClient,
        ChatRequestDto request,
        CancellationToken ct
    )
    {
        IEnumerable<ChatMessage> messages =
        [
            new ChatMessage(
                Microsoft.Extensions.AI.ChatRole.System,
                @"
You are an assistant who answers questions about health insurance.
Do not answer questions about anything else.
Use only simple markdown to format your responses.

Use the Search tool to find relevant information. When you do this, end your
reply with citations in the special XML format:

<citation filename='string'>exact quote here</citation>

Always include the citation in your response if there are results.

The quote must be max 5 words, taken word-for-word from the search result, and is the basis for why the citation is relevant.
Don't refer to the presence of citations; just emit these tags right at the end, with no surrounding text.
"
            ),
            .. request
                .Messages.Where(m => m.Role != ChatRole.System) // don't let the user add system messages
                .Select(m => new ChatMessage(ChatRoleMapper.Map(m.Role), m.Text)),
        ];

        var chatOptions = new ChatOptions();

#pragma warning disable CS8321 // Local function is declared but never used
        async IAsyncEnumerable<SseItem<string>> StreamChat(
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            await foreach (
                var update in chatClient.GetStreamingResponseAsync(
                    messages,
                    chatOptions,
                    cancellationToken
                )
            )
            {
                if (!string.IsNullOrEmpty(update.Text))
                {
                    yield return new SseItem<string>(update.Text, eventType: "chat")
                    {
                        ReconnectionInterval = System.Diagnostics.Debugger.IsAttached
                            ? TimeSpan.FromSeconds(100)
                            : TimeSpan.FromSeconds(10),
                    };
                }
            }
        }
#pragma warning restore CS8321 // Local function is declared but never used

        return TypedResults.ServerSentEvents(StreamChat(ct));
    }

    public static IChatClient CreateGithubModelsChatClient(string apiKey)
    {
        var credential = new ApiKeyCredential(
            apiKey
                ?? throw new InvalidOperationException(
                    "Missing configuration: GitHubModels:Token. See the README for details."
                )
        );
        var openAIOptions = new OpenAIClientOptions()
        {
            Endpoint = new Uri("https://models.inference.ai.azure.com"),
        };

        var ghModelsClient = new OpenAIClient(credential, openAIOptions);
        var chatClient = ghModelsClient.GetChatClient("gpt-4o-mini").AsIChatClient();
        return chatClient;
    }

#pragma warning disable CS8321 // Local function is declared but never used
    private static async IAsyncEnumerable<SseItem<string>> StreamChatDummy(
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        for (int i = 0; i < 10; i++)
        {
            yield return new SseItem<string>($"Dummy chunk {i + 1}/10\n", eventType: "chat")
            {
                ReconnectionInterval = System.Diagnostics.Debugger.IsAttached
                    ? TimeSpan.FromSeconds(100)
                    : TimeSpan.FromSeconds(10),
            };
            await Task.Delay(200, cancellationToken);
        }
    }
#pragma warning restore CS8321 // Local function is declared but never used
}

public static class ChatRoleMapper
{
    public static Microsoft.Extensions.AI.ChatRole Map(ChatRole role) =>
        role switch
        {
            ChatRole.System => Microsoft.Extensions.AI.ChatRole.System,
            ChatRole.User => Microsoft.Extensions.AI.ChatRole.User,
            ChatRole.Assistant => Microsoft.Extensions.AI.ChatRole.Assistant,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
        };
}
