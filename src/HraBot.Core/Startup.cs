using Amazon.Lambda.Annotations;
using HraBot.ServiceDefaults;
using Microsoft.Extensions.DependencyInjection;

namespace HraBot.Core;

[LambdaStartup]
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .RegisterAiServices(
                Environment.GetEnvironmentVariable($"ConnectionStrings__{HraServices.vectorDb}")
                    ?? throw new InvalidOperationException("Could not find connection for vectorDb")
            )
            .AddInfrastructure(
                Environment.GetEnvironmentVariable($"ConnectionStrings__{HraServices.postgres}")
                    ?? throw new InvalidOperationException("Could not find connection for postgres")
            );
    }
}
