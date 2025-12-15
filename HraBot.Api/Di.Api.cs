using System;
using HraBot.Api.Features.Agents;
using HraBot.Api.Features.Workflows;

namespace HraBot.Api;

public static class Di_Api
{
    public static void RegisterAiServices(this IServiceCollection services)
    {
        services.AddHraBotAgent();
        services.AddCitationValidationBot();
        services.AddTransient<ReturnApprovedResponse>();
        services.AddTransient<HraBotExecutor>();
        services.AddTransient<CitationValidatorExecutor>();
    }
}
