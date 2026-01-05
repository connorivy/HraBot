using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using HraBot.ApiClient;
using HraBot.ServiceDefaults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace HraBot.Tests.E2E;

public class SetupTestsE2E
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan SetupTimeout = TimeSpan.FromSeconds(240);
    public static DistributedApplication AppHost
    {
        get => field ?? throw new InvalidOperationException("Apphost has not been set");
        private set;
    }

    private static HttpClient BackendHttpClient
    {
        get => field ?? throw new InvalidOperationException("ApiClient has not been set");
        set;
    }

    public static string FrontendAddress
    {
        get => field ?? throw new InvalidOperationException("Frontend endpoint is not set");
        private set;
    }

    private static async Task<DistributedApplication> CreateAppHost()
    {
        var hostBuilder =
            await DistributedApplicationTestingBuilder.CreateAsync<Projects.HraBot_AppHost>(
                [
                    $"TestOverrides:Resources:{AppServices.API}:Environment:{AppOptions.MockChatClient_bool}=true",
                    $"TestOverrides:Resources:{AppServices.MIGRATION_SERVICE}:Environment:{AppOptions.ENV_NAME_IsEphemeralDb}=true",
                ],
                (options, settings) => { }
            );

        hostBuilder.Services.AddLogging(l => l.AddConsole());

        // if (!System.Diagnostics.Debugger.IsAttached)
        // {
        //     var pgadminResource = hostBuilder.Resources.First(r => r.Name == AppServices.PG_ADMIN);
        //     hostBuilder.Resources.Remove(pgadminResource);
        //     var markitdown = hostBuilder.Resources.First(r => r.Name == AppServices.MARK_IT_DOWN);
        //     hostBuilder.Resources.Remove(markitdown);
        // }
        // make postgres container ephemeral
        var postgres = hostBuilder.Resources.First(r => r.Name == AppServices.postgres);
        var lifetime = postgres.Annotations.OfType<ContainerLifetimeAnnotation>().FirstOrDefault();
        if (lifetime is not null)
        {
            postgres.Annotations.Remove(lifetime);
        }

        var app = await hostBuilder.BuildAsync().WaitAsync(SetupTimeout, CancellationToken.None);
        await app.StartAsync(CancellationToken.None).WaitAsync(SetupTimeout);
        return app;
    }

    public static HraBotApiClient ApiClient =>
        new(
            new HttpClientRequestAdapter(
                new AnonymousAuthenticationProvider(),
                httpClient: BackendHttpClient
            )
        );

    [Before(HookType.Assembly)]
    public static async Task AssemblySetup()
    {
        AppHost = await CreateAppHost();
        BackendHttpClient = AppHost.CreateHttpClient(AppServices.API);
        await AppHost.ResourceNotifications.WaitForResourceHealthyAsync(
            AppServices.WEB,
            CancellationToken.None
        );
        FrontendAddress = AppHost.GetEndpoint(AppServices.WEB).AbsoluteUri;
        Console.WriteLine($"Frontend address {FrontendAddress}");
    }

    [After(HookType.Assembly)]
    public static async Task AssemblyTeardown()
    {
        await AppHost.StopAsync(CancellationToken.None);
        AppHost.Dispose();
        BackendHttpClient?.Dispose();
    }
}
