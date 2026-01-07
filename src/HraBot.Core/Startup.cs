using Amazon.Lambda.Annotations;
using HraBot.ServiceDefaults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HraBot.Core;

[LambdaStartup]
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(l => l.AddLambdaLogger());
        services
            .AddAiServices(
                Environment.GetEnvironmentVariable($"ConnectionStrings__{AppServices.vectorDb}")
                    ?? throw new InvalidOperationException("Could not find connection for vectorDb")
            )
            .AddInfrastructure(
                Environment.GetEnvironmentVariable($"ConnectionStrings__{AppServices.hraBotDb}")
                    ?? throw new InvalidOperationException("Could not find connection for postgres")
            );
    }

    // public void ConfigureHostBuilder(IServiceCollection services)
    // {
    //     services
    //         .AddAiServices(
    //             Environment.GetEnvironmentVariable($"ConnectionStrings__{AppServices.vectorDb}")
    //                 ?? throw new InvalidOperationException("Could not find connection for vectorDb")
    //         )
    //         .AddInfrastructure(
    //             Environment.GetEnvironmentVariable($"ConnectionStrings__{AppServices.hraBotDb}")
    //                 ?? throw new InvalidOperationException("Could not find connection for postgres")
    //         );
    // }
}
