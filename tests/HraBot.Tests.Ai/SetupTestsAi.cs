using Aspire.Hosting;
using Aspire.Hosting.Testing;
using HraBot.ServiceDefaults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HraBot.Tests.Ai;

public class SetupTestsAi
{
    public static DistributedApplication AppHost
    {
        get => field ?? throw new InvalidOperationException("Apphost has not been set");
        private set;
    }

    private static async Task<DistributedApplication> CreateAppHost()
    {
        var hostBuilder =
            await DistributedApplicationTestingBuilder.CreateAsync<Projects.HraBot_AppHost>();

        var app = await hostBuilder.BuildAsync().WaitAsync(CancellationToken.None);
        await app.StartAsync(CancellationToken.None);
        return app;
    }

    public static HttpClient ApiClient
    {
        get => field ?? throw new InvalidOperationException("ApiClient has not been set");
        private set;
    }

    [Before(HookType.Assembly)]
    public static async Task AssemblySetup()
    {
        AppHost = await CreateAppHost();
        ApiClient = AppHost.CreateHttpClient(AppServices.API);
        await AppHost.ResourceNotifications.WaitForResourceHealthyAsync(
            AppServices.API,
            CancellationToken.None
        );

        InitOpenAiKeys();
    }

    private static List<string> OpenAiApiKeys = [];

    /// <summary>
    /// Please don't judge me for using multiple API keys
    /// </summary>
    private static void InitOpenAiKeys()
    {
        var config = AppHost.Services.GetRequiredService<IConfiguration>();

        int keyNum = 0;
        while (
            config[$"ConnectionStrings:openai{(keyNum == 0 ? "" : keyNum.ToString())}"]
                is string apiKey
        )
        {
            // will get api key in this format "Endpoint=<endpoint>;Key=<key>"
            // extract just the key part without the "Key=" prefix
            OpenAiApiKeys.Add(apiKey.Split(';').First(part => part.StartsWith("Key="))[4..]);
            keyNum++;
        }
    }

    public static string GetOpenAiApiKey()
    {
        var rand = new Random();
        int index = rand.Next(OpenAiApiKeys.Count);
        return OpenAiApiKeys[index];
    }

    [After(HookType.Assembly)]
    public static async Task AssemblyTeardown()
    {
        if (AppHost != null)
        {
            await AppHost.StopAsync(CancellationToken.None);
            AppHost.Dispose();
        }
        ApiClient?.Dispose();
    }
}
