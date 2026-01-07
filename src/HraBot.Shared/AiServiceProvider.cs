using Azure.AI.OpenAI;
using GenerativeAI;
using GenerativeAI.Microsoft;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

namespace HraBot.Shared;

public abstract class AiServiceProvider
{
    public abstract IChatClient GetChatClient();
    public abstract IEmbeddingGenerator<string, Embedding<float>> GetEmbeddingGenerator();

    protected static OpenAiConfigInfo ParseOpenAiConnectionString(string connectionString)
    {
        string? endpoint = null;
        string? apiKey = null;

        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var segments = part.Split('=', 2);
            if (segments.Length != 2)
            {
                continue;
            }

            if (segments[0].Equals("Endpoint", StringComparison.OrdinalIgnoreCase))
            {
                endpoint = segments[1];
            }
            else if (segments[0].Equals("Key", StringComparison.OrdinalIgnoreCase))
            {
                apiKey = segments[1];
            }
        }

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OpenAI connection string must include Endpoint and Key."
            );
        }

        return new OpenAiConfigInfo(endpoint, apiKey);
    }
}

public class SingleAiServiceProvider : AiServiceProvider
{
    private AzureOpenAIClient OpenAiClient
    {
        get
        {
            if (field == null)
            {
                var openaiConnectionString =
                    Environment.GetEnvironmentVariable($"ConnectionStrings__openai")
                    ?? throw new InvalidOperationException(
                        "Could not find openai connection string"
                    );
                var openAiConfig = ParseOpenAiConnectionString(openaiConnectionString);
                field = new AzureOpenAIClient(
                    new Uri(openAiConfig.Endpoint),
                    new System.ClientModel.ApiKeyCredential(openAiConfig.ApiKey)
                );
            }
            return field;
        }
    }

    public override IChatClient GetChatClient()
    {
        return this.OpenAiClient.GetChatClient("gpt-4o-mini").AsIChatClient();
    }

    public override IEmbeddingGenerator<string, Embedding<float>> GetEmbeddingGenerator()
    {
        return this
            .OpenAiClient.GetEmbeddingClient("text-embedding-3-small")
            .AsIEmbeddingGenerator();
    }
}

public class MultiAiServiceProvider(AiConfigInfoProvider configInfoProvider) : AiServiceProvider
{
    private List<IChatClient> AllChatClients
    {
        get => field ??= [.. GetAllChatClients()];
    }

    private List<IEmbeddingGenerator<string, Embedding<float>>> AllEmbeddingGenerators
    {
        get => field ??= [.. CreateEmbeddingGenerator()];
    }

    public override IChatClient GetChatClient()
    {
        if (AllChatClients.Count == 0)
        {
            throw new InvalidOperationException("No Chat Clients are configured.");
        }
        var rand = new Random();
        int index = rand.Next(AllChatClients.Count);
        return AllChatClients[index];
    }

    public override IEmbeddingGenerator<string, Embedding<float>> GetEmbeddingGenerator()
    {
        var rand = new Random();
        int index = rand.Next(AllEmbeddingGenerators.Count);
        return AllEmbeddingGenerators[index];
    }

    private IEnumerable<IChatClient> GetAllChatClients()
    {
        foreach (var openAiConfig in configInfoProvider.GetAllOpenAiConfigInfos())
        {
            var client = new AzureOpenAIClient(
                new Uri(openAiConfig.Endpoint),
                new System.ClientModel.ApiKeyCredential(openAiConfig.ApiKey)
            )
                .GetChatClient("gpt-4o-mini")
                .AsIChatClient();
            yield return client;
        }

        foreach (var geminiConfig in configInfoProvider.GetAllGeminiConfigInfos())
        {
            var client = new GenerativeAIChatClient(
                geminiConfig.ApiKey,
                GoogleAIModels.GeminiFlashLatest
            );
            yield return client;
        }
    }

    private IEnumerable<IEmbeddingGenerator<string, Embedding<float>>> CreateEmbeddingGenerator()
    {
        foreach (var openAiConfig in configInfoProvider.GetAllOpenAiConfigInfos())
        {
            var client = new AzureOpenAIClient(
                new Uri(openAiConfig.Endpoint),
                new System.ClientModel.ApiKeyCredential(openAiConfig.ApiKey)
            )
                .GetEmbeddingClient("text-embedding-3-small")
                .AsIEmbeddingGenerator();
            yield return client;
        }
    }
}

public class AiConfigInfoProvider(IConfiguration config)
{
    private List<OpenAiConfigInfo> OpenAiConfigInfos
    {
        get => field ??= GetOpenAiConfigInfosFromConfig();
        set;
    }

    private List<GeminiConfigInfo> GeminiConfigInfos
    {
        get => field ??= GetGeminiConfigInfosFromConfig();
        set;
    }

    private List<GeminiConfigInfo> GetGeminiConfigInfosFromConfig()
    {
        List<GeminiConfigInfo> geminiApiKeys = [];
        int keyNum = 0;
        while (config[$"ApiKeys:gemini{(keyNum == 0 ? "" : keyNum.ToString())}"] is string apiKey)
        {
            geminiApiKeys.Add(new GeminiConfigInfo(apiKey));
            keyNum++;
        }
        return geminiApiKeys;
    }

    private List<OpenAiConfigInfo> GetOpenAiConfigInfosFromConfig()
    {
        List<OpenAiConfigInfo> openAiApiKeys = [];
        int keyNum = 0;
        while (
            config[$"ConnectionStrings:openai{(keyNum == 0 ? "" : keyNum.ToString())}"]
                is string apiKey
        )
        {
            openAiApiKeys.Add(ParseOpenAiConnectionString(apiKey));
            keyNum++;
        }
        return openAiApiKeys;
    }

    public OpenAiConfigInfo GetOpenAiConfigInfo()
    {
        var rand = new Random();
        int index = rand.Next(OpenAiConfigInfos.Count);
        return OpenAiConfigInfos[index];
    }

    public IReadOnlyList<OpenAiConfigInfo> GetAllOpenAiConfigInfos() =>
        OpenAiConfigInfos.AsReadOnly();

    public IReadOnlyList<GeminiConfigInfo> GetAllGeminiConfigInfos() =>
        GeminiConfigInfos.AsReadOnly();

    private static OpenAiConfigInfo ParseOpenAiConnectionString(string connectionString)
    {
        string? endpoint = null;
        string? apiKey = null;

        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var segments = part.Split('=', 2);
            if (segments.Length != 2)
            {
                continue;
            }

            if (segments[0].Equals("Endpoint", StringComparison.OrdinalIgnoreCase))
            {
                endpoint = segments[1];
            }
            else if (segments[0].Equals("Key", StringComparison.OrdinalIgnoreCase))
            {
                apiKey = segments[1];
            }
        }

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OpenAI connection string must include Endpoint and Key."
            );
        }

        return new OpenAiConfigInfo(endpoint, apiKey);
    }
}

public record OpenAiConfigInfo(string Endpoint, string ApiKey);

public record GeminiConfigInfo(string ApiKey);
