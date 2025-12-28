using System.Diagnostics.CodeAnalysis;
using HraBot.Api.Features.Agents;
using HraBot.Api.Features.Workflows;
using HraBot.Api.Services;
using HraBot.Api.Services.Ingestion;
using HraBot.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceScan.SourceGenerator;

namespace HraBot.Core;

public static partial class Di_Core
{
    public static IServiceCollection RegisterAllServices(
        this IServiceCollection services,
        string connectionString
    )
    {
        return services.RegisterAiServices().AddInfrastructure(connectionString);
    }

    public static IServiceCollection RegisterAiServices(this IServiceCollection services)
    {
        services
            .AddChatClient(sp => sp.GetRequiredService<AiServiceProvider>().GetRandomChatClient())
            .UseFunctionInvocation()
            .UseOpenTelemetry(
#if DEBUG
                configure: c => c.EnableSensitiveData = true
#endif
            );

        services.AddEmbeddingGenerator(sp =>
            sp.GetRequiredService<AiServiceProvider>().GetRandomEmbeddingGeneratorClient()
        );

        // builder.AddQdrantClient(HraServices.vectorDb);
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
        services.AddQdrantVectorStore();
        services.AddQdrantCollection<Guid, IngestedChunk>(IngestedChunk.CollectionName);
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
        services.AddSingleton<DataIngestor>();
        services.AddSingleton<SemanticSearch>();
        // services.AddKeyedSingleton(
        //     "ingestion_directory",
        //     new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "Data"))
        // );
        services.AddHraBotAgent();
        services.AddCitationValidationBot();
        services.AddSearchAgent();
        // app.AddWorkflow(WorkflowNames.Review, (sp, _) => ReturnApprovedResponse.CreateWorkflow(sp))
        //     .AddAsAIAgent();
        services.AddTransient<ReturnApprovedResponse>();
        services.AddTransient<SearchBotExecutor>();
        services.AddSingleton<HraBotExecutor>();
        services.AddSingleton<CitationValidatorExecutor>();
        services.AddSingleton<AiServiceProvider>();
        services.AddSingleton<AiConfigInfoProvider>();
        // services.AddScoped<ThreadProvider>();
        services.AddSingleton<AgentLogger>();
        return services;
    }

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString
    )
    {
        services.AddDbContextPool<HraBotDbContext>(o =>
        {
            o.UseNpgsql(connectionString)
#if DEBUG
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
                .LogTo(Console.WriteLine, LogLevel.Information)
#endif
            ;
        });

        return services;
    }

    [GenerateServiceRegistrations(
        AssignableTo = typeof(IEntityTypeConfiguration<>),
        CustomHandler = nameof(ApplyConfiguration)
    )]
    public static partial ModelBuilder AddEntityConfigurations(this ModelBuilder builder);

    private static void ApplyConfiguration<
        TConfig,
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicConstructors
                | DynamicallyAccessedMemberTypes.NonPublicConstructors
                | DynamicallyAccessedMemberTypes.PublicFields
                | DynamicallyAccessedMemberTypes.NonPublicFields
                | DynamicallyAccessedMemberTypes.PublicProperties
                | DynamicallyAccessedMemberTypes.NonPublicProperties
                | DynamicallyAccessedMemberTypes.Interfaces
        )]
            TEntity
    >(ModelBuilder builder)
        where TConfig : IEntityTypeConfiguration<TEntity>, new()
        where TEntity : class
    {
        _ = builder.ApplyConfiguration(new TConfig());
    }
}
