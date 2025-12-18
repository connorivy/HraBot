using System;
using Microsoft.Extensions.Hosting;

namespace HraBot.Shared;

public static class IHostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddVectorDb(this IHostApplicationBuilder builder)
    {
        // Implementation for adding vector database connection
        return builder;
    }
}