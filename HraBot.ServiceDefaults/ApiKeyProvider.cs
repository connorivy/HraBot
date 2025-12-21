using System;
using Microsoft.Extensions.Configuration;

namespace HraBot.ServiceDefaults;

/// <summary>
/// Please don't judge me for using multiple API keys
/// </summary>
public class ApiKeyProvider(IConfiguration config)
{
    private List<string> OpenAiApiKeys
    {
        get => field ??= GetOpenAiApiKeysFromConfig();
        set;
    }

    private List<string> GeminiApiKeys
    {
        get => field ??= GetGeminiApiKeysFromConfig();
        set;
    }

    private List<string> GetGeminiApiKeysFromConfig()
    {
        List<string> geminiApiKeys = [];
        int keyNum = 0;
        while (config[$"ApiKeys:gemini{(keyNum == 0 ? "" : keyNum.ToString())}"] is string apiKey)
        {
            geminiApiKeys.Add(apiKey);
            keyNum++;
        }
        return geminiApiKeys;
    }

    private List<string> GetOpenAiApiKeysFromConfig()
    {
        List<string> openAiApiKeys = [];
        int keyNum = 0;
        while (
            config[$"ConnectionStrings:openai{(keyNum == 0 ? "" : keyNum.ToString())}"]
                is string apiKey
        )
        {
            // will get api key in this format "Endpoint=<endpoint>;Key=<key>"
            // extract just the key part without the "Key=" prefix
            openAiApiKeys.Add(apiKey.Split(';').First(part => part.StartsWith("Key="))[4..]);
            keyNum++;
        }
        return openAiApiKeys;
    }

    public string GetOpenAiApiKey()
    {
        var rand = new Random();
        int index = rand.Next(OpenAiApiKeys.Count);
        return OpenAiApiKeys[index];
    }

    public IReadOnlyList<string> GetAllOpenAiApiKeys() => OpenAiApiKeys.AsReadOnly();
}
