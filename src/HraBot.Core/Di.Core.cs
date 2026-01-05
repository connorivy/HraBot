using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using HraBot.Api.Features.Agents;
using HraBot.Api.Features.Workflows;
using HraBot.Api.Services;
using HraBot.Api.Services.Ingestion;
using HraBot.Core.Common;
using HraBot.ServiceDefaults;
using HraBot.Shared;
using Microsoft.Agents.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Qdrant.Client;
using ServiceScan.SourceGenerator;
#if !GENERATING_EF
using HraBot.Core.Generated.EF;
#endif

namespace HraBot.Core;

public static partial class Di_Core
{
    public static IServiceCollection RegisterAllServices(
        this IServiceCollection services,
        string qdrantConnectionString,
        string postgresConnectionString
    )
    {
        return services
            .AddEndpoints()
            .AddAiServices(qdrantConnectionString)
            .AddInfrastructure(postgresConnectionString);
    }

    [GenerateServiceRegistrations(
        AssignableTo = typeof(BaseEndpoint<,>),
        Lifetime = ServiceLifetime.Scoped,
        AsSelf = true
    )]
    public static partial IServiceCollection AddEndpoints(this IServiceCollection services);

    public static IServiceCollection AddAiServices(
        this IServiceCollection services,
        string qdrantConnectionString
    )
    {
        if (
            Environment.GetEnvironmentVariable(AppOptions.MockChatClient_bool)
                is string mockChatClientString
            && bool.TryParse(mockChatClientString, out var mockChatClient)
            && mockChatClient
        )
        {
            return services.AddDumbAiServices();
        }
        return services.AddRealAiServices(qdrantConnectionString);
    }

    public static IServiceCollection AddRealAiServices(
        this IServiceCollection services,
        string qdrantConnectionString
    )
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

        var (endpoint, key) = ExtractEndpointAndKey(qdrantConnectionString);
        services.AddSingleton(sp => new QdrantClient(
            new Uri(endpoint),
            apiKey: key,
            loggerFactory: sp.GetRequiredService<ILoggerFactory>()
        ));
        services.AddHraBotQdrantVectorStore();
        services.AddHraBotQdrantCollection(
            IngestedChunk.CollectionName,
            optionsProvider: (sp) =>
                new QdrantCollectionOptions()
                {
                    Definition = new VectorStoreCollectionDefinition()
                    {
                        Properties =
                        [
                            new VectorStoreKeyProperty("id", typeof(Guid)),
                            new VectorStoreDataProperty("documentid", typeof(string)),
                            new VectorStoreDataProperty("context", typeof(string)),
                            new VectorStoreDataProperty("content", typeof(string)),
                            new VectorStoreVectorProperty(
                                "embedding",
                                typeof(string),
                                IngestedChunk.VectorDimensions
                            )
                            {
                                DistanceFunction = IngestedChunk.VectorDistanceFunction,
                            },
                        ],
                    },
                }
        );
        services.AddSingleton<DataIngestor>();
        services.AddSingleton<SemanticSearch>();
        services.AddKeyedSingleton(
            "ingestion_directory",
            new DirectoryInfo(Path.Combine(GetProjectRoot(), "Data"))
        );
        services.AddQueryRewriteAgent();
        services.AddHraBotAgent();
        services.AddCitationValidationBot();
        // services.AddSearchAgent();
        services.AddSingleton<AiServiceProvider>();
        services.AddSingleton<AiConfigInfoProvider>();
        services.AddWorkflowAsAgent(
            WorkflowNames.Review,
            (sp, _) => GetApprovedResponseWorkflow.CreateWorkflow(sp)
        );
        services.AddTransient<GetApprovedResponseWorkflow>();
        services.AddSingleton<QueryRewriteExecutor>();
        services.AddSingleton<HraBotExecutor>();
        services.AddSingleton<CitationValidatorExecutor>();
        services.AddSingleton<MultiQuerySearchExecutor>();
        // services.AddTransient<SearchBotExecutor>();
        services.AddSingleton<AgentLogger>();
        return services;
    }

    public static IServiceCollection AddDumbAiServices(this IServiceCollection services)
    {
        return services.AddSingleton<
            GetApprovedResponseWorkflow,
            GetDummyApprovedResponseWorkflow
        >();
    }

    static string GetProjectRoot()
    {
        var dir = AppContext.BaseDirectory; // bin/Debug/net8.0/
        return Directory.GetParent(dir)!.Parent!.Parent!.FullName;
    }

    /// <summary>
    /// Given a connection string in this format "Endpoint={{value}};Key={{value}}"
    /// Parse the inner values and return them as a tuple
    /// </summary>
    /// <param name="connectionString"></param>
    /// <returns></returns>
    static (string Endpoint, string Key) ExtractEndpointAndKey(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException(
                "Connection string cannot be null or empty.",
                nameof(connectionString)
            );
        }

        var match = ParseEndpointAndKeyFromConnectionString().Match(connectionString);

        var endpoint = match.Success ? match.Groups["endpoint"].Value.Trim() : null;
        Console.WriteLine(endpoint);
        var key = match.Success ? match.Groups["key"].Value.Trim() : null;
        Console.WriteLine(key);

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException(
                "Connection string must include a non-empty 'Endpoint' entry.",
                nameof(connectionString)
            );
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException(
                "Connection string must include a non-empty 'Key' entry.",
                nameof(connectionString)
            );
        }

        return (endpoint, key);
    }

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString
    )
    {
        services.AddDbContextPool<HraBotDbContext>(o =>
        {
            o.UseNpgsql(connectionString)
#if !GENERATING_EF
                .UseModel(HraBotDbContextModel.Instance)
#endif
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

    public static void AddAIAgent(
        this IServiceCollection services,
        string name,
        Func<IServiceProvider, string, AIAgent> createAgentDelegate
    )
    {
        services.AddKeyedSingleton(
            name,
            (sp, key) =>
            {
                var keyString =
                    key as string
                    ?? throw new InvalidOperationException("Ai Agent key cannot be null");
                var agent =
                    createAgentDelegate(sp, keyString)
                    ?? throw new InvalidOperationException(
                        $"The agent factory did not return a valid {nameof(AIAgent)} instance for key '{keyString}'."
                    );
                if (!string.Equals(agent.Name, keyString, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"The agent factory returned an agent with name '{agent.Name}', but the expected name is '{keyString}'."
                    );
                }

                return agent;
            }
        );
    }

    [GeneratedRegex(
        @"(?:^|;)\s*Endpoint\s*=\s*(?<endpoint>[^;]+)\s*;?\s*(?:Key\s*=\s*(?<key>[^;]+))?|(?:^|;)\s*Key\s*=\s*(?<key>[^;]+)\s*;?\s*(?:Endpoint\s*=\s*(?<endpoint>[^;]+))?",
        RegexOptions.IgnoreCase,
        "en-US"
    )]
    private static partial Regex ParseEndpointAndKeyFromConnectionString();
}
