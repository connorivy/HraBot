using System;
using Amazon.Lambda.Annotations;
using HraBot.Core.Features.Chat;
using Microsoft.Extensions.DependencyInjection;

namespace HraBot.Core;

[LambdaStartup]
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.RegisterAiServices().AddInfrastructure("");
    }
}
