using System;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using HraBot.Api;

namespace HraBot.Shared;

public static class HttpExtensions
{
    public static async IAsyncEnumerable<string> StreamChatResponse(
        this HttpClient httpClient, 
        ChatRequestDto chatRequest,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/chat/stream")
        {
            Content = JsonContent.Create(chatRequest)
        };
        requestMessage.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var response = await httpClient!.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead,
        cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6);
                yield return data;
            }
        }
    }
}
