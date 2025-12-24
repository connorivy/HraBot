using System;
using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;

namespace HraBot.Shared;

public static class ChatClientUtils
{
    public static IChatClient CreateGithubModelsChatClient(string apiKey, string modelName)
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
        var chatClient = ghModelsClient.GetChatClient(modelName).AsIChatClient();
        return chatClient;
    }
}
