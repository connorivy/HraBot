using System;
using System.Reflection;
using HraBot.Api.Features.Agents;
using HraBot.Api.Features.Workflows;
using HraBot.Core.Common;
using HraBot.Core.Features.Chat;
using HraBot.Shared;
using Microsoft.Agents.AI.Hosting;
using ServiceScan.SourceGenerator;

namespace HraBot.Api;

public static partial class Di_Api
{
    public static void RegisterAiServices(this WebApplicationBuilder app)
    {
        app.Services.AddHraBotAgent();
        app.Services.AddCitationValidationBot();
        app.Services.AddSearchAgent();
        app.AddWorkflow(WorkflowNames.Review, (sp, _) => ReturnApprovedResponse.CreateWorkflow(sp))
            .AddAsAIAgent();
        app.Services.AddTransient<ReturnApprovedResponse>();
        app.Services.AddTransient<SearchBotExecutor>();
        app.Services.AddSingleton<HraBotExecutor>();
        app.Services.AddSingleton<CitationValidatorExecutor>();
        app.Services.AddSingleton<AiServiceProvider>();
        app.Services.AddSingleton<AiConfigInfoProvider>();
        // app.Services.AddScoped<ThreadProvider>();
        app.Services.AddSingleton<AgentLogger>();
    }

    [GenerateServiceRegistrations(
        AssignableTo = typeof(IBaseEndpoint),
        CustomHandler = nameof(IBaseEndpoint.Configure),
        FromAssemblyOf = typeof(BaseEndpoint<,>)
    )]
    public static partial void MapEndpoints(this IEndpointRouteBuilder app);

    public static void ConfigureWebApiRoute(this IEndpointRouteBuilder app) { }

    public static void Map<TEndpoint, TRequest, TResponse>(IEndpointRouteBuilder app)
        where TEndpoint : BaseEndpoint<TRequest, TResponse>
    {
        var endpoint =
            typeof(TEndpoint).GetCustomAttribute<HraBotEndpointAttribute>()
            ?? throw new InvalidOperationException(
                $"Class {typeof(TEndpoint).Name} is missing the endpoint attribute"
            );
        var route = endpoint.Route;
        var endpointType = endpoint.Http;

        Func<string, Delegate, RouteHandlerBuilder> mapFunc = endpointType switch
        {
            Http.Delete => app.MapDelete,
            Http.Get => app.MapGet,
            Http.Patch => app.MapPatch,
            Http.Post => app.MapPost,
            Http.Put => app.MapPut,
            _ => throw new NotImplementedException(),
        };

        Delegate mapDelegate;
        if (!route.Contains('{'))
        {
            mapDelegate = async (
                [Microsoft.AspNetCore.Mvc.FromBody] TRequest req,
                IServiceProvider serviceProvider
            ) =>
            {
                var endpoint = serviceProvider.GetRequiredService<TEndpoint>();
#if GENERATING_OPENAPI
                return endpoint.ReturnResponse();
#else
                return await endpoint.ExecuteAsync(req);
#endif
            };
        }
        else
        {
            mapDelegate = async ([AsParameters] TRequest req, IServiceProvider serviceProvider) =>
            {
                var endpoint = serviceProvider.GetRequiredService<TEndpoint>();
#if GENERATING_OPENAPI
                return endpoint.ReturnResponse();
#else
                return await endpoint.ExecuteAsync(req);
#endif
            };
        }

        var endpointBuilder = mapFunc(route, mapDelegate);

        // endpointBuilder.WithName(typeof(TEndpoint).Name);
        // foreach (var tag in tags)
        // {
        //     endpointBuilder.WithTags(tag.Value);
        // }

        // endpointBuilder.ProducesProblem(404);
    }
}
