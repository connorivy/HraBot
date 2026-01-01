using Microsoft.Extensions.AI;

namespace HraBot.Core.Features.Chat;

public class DummyChatClient(IServiceProvider serviceProvider) : IChatClient
{
    public void Dispose() { }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult(
            new ChatResponse(new ChatMessage(ChatRole.Assistant, "This is a chat client mock")) { }
        );
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return serviceProvider.GetService(serviceType);
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        var response = new ChatResponse(
            new ChatMessage(ChatRole.Assistant, "This is a chat client mock")
        );

        return StreamResponse(response, cancellationToken);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> StreamResponse(
        ChatResponse response,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        foreach (var update in response.ToChatResponseUpdates())
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return update;
            await Task.Yield();
        }
    }
}
