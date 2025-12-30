using System;
using System.Reflection;
using HraBot.Api.Features.Agents;
using HraBot.Api.Features.Workflows;
using HraBot.Core.Common;
using HraBot.Shared;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.AspNetCore.Mvc;
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
        AssignableTo = typeof(BaseEndpoint<,>),
        CustomHandler = nameof(ApplyConfiguration),
        FromAssemblyOf = typeof(BaseEndpoint<,>)
    )]
    public static partial void AddEntityConfigurations(this WebApplication app);

    private static void ApplyConfiguration<TEndpoint, TRequest, TResponse>(WebApplication app)
        where TEndpoint : BaseEndpoint<TRequest, TResponse>
    {
        // app.MapPost(
        //     "/api/hrabot",
        //     async (
        //         [FromServices] TEndpoint workflow,
        //         [FromBody] ChatRequestDto request,
        //         CancellationToken ct
        //     ) =>
        //         await workflow.GetApprovedResponse(
        //             request.Messages.Select(m => new ChatMessage(
        //                 ChatRoleMapper.Map(m.Role),
        //                 m.Text
        //             )),
        //             ct
        //         )
        // );
    }

    //     public static void Map<TEndpoint, TRequest, TResponse>(IEndpointRouteBuilder app)
    //         where TEndpoint : BaseEndpoint<TRequest, TResponse>
    //     {
    //         string route =
    //             typeof(TEndpoint).GetCustomAttribute<HraBotRouteAttribute>()?.Value
    //             ?? throw new InvalidOperationException(
    //                 $"Class {typeof(TEndpoint).Name} is missing the route attribute"
    //             );
    //         string endpointType =
    //             typeof(TEndpoint).GetCustomAttribute<HraBotEndpointTypeAttribute>()?.Value
    //             ?? throw new InvalidOperationException(
    //                 $"Class {typeof(TEndpoint).Name} is missing the route attribute"
    //             );
    //         var tags = typeof(TEndpoint).GetCustomAttributes<HraBotTagAttribute>();

    //         Func<string, Delegate, RouteHandlerBuilder> mapFunc = endpointType switch
    //         {
    //             Http.Delete => app.MapDelete,
    //             Http.Get => app.MapGet,
    //             Http.Patch => app.MapPatch,
    //             Http.Post => app.MapPost,
    //             Http.Put => app.MapPut,
    //             _ => throw new NotImplementedException(),
    //         };

    //         Delegate mapDelegate;
    //         if (
    //             Common.Application.DependencyInjection.ConcreteTypeDerivedFromBase(
    //                 typeof(TEndpoint),
    //                 typeof(HraBotFromBodyResultBaseEndpoint<,>)
    //             )
    //         // || Common.Application.DependencyInjection.ConcreteTypeDerivedFromBase(
    //         //     typeof(TEndpoint),
    //         //     typeof(HraBotFromBodyBaseEndpoint<,>)
    //         // )
    //         )
    //         {
    //             mapDelegate = async (
    //                 [Microsoft.AspNetCore.Mvc.FromBody] TRequest req,
    //                 IServiceProvider serviceProvider
    //             ) =>
    //             {
    //                 var endpoint = serviceProvider.GetRequiredService<TEndpoint>();
    // #if CODEGEN
    //                 return await endpoint.GetResponseTypeForClientGenerationPurposes();
    // #else
    //                 return (await endpoint.ExecuteAsync(req)).ToWebResult();
    // #endif
    //             };
    //         }
    //         else
    //         {
    //             mapDelegate = async ([AsParameters] TRequest req, IServiceProvider serviceProvider) =>
    //             {
    //                 var endpoint = serviceProvider.GetRequiredService<TEndpoint>();
    // #if CODEGEN
    //                 return await endpoint.GetResponseTypeForClientGenerationPurposes();
    // #else
    //                 return await endpoint.ExecuteAsync(req);
    // #endif
    //             };
    //         }

    //         var endpointBuilder = mapFunc(route, mapDelegate);

    //         endpointBuilder.WithName(typeof(TEndpoint).Name);
    //         foreach (var tag in tags)
    //         {
    //             endpointBuilder.WithTags(tag.Value);
    //         }

    //         // endpointBuilder.ProducesProblem(404);
    //     }
}
