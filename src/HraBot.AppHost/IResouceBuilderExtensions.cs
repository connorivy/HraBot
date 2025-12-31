using Microsoft.Extensions.Configuration;

namespace HraBot.AppHost;

public static class IResouceBuilderExtensions
{
    public static IResourceBuilder<TResouce> ApplyTestEnvironmentOverrides<TResouce>(
        this IResourceBuilder<TResouce> resource
    )
        where TResouce : IResourceWithEnvironment
    {
        var overrides = resource.ApplicationBuilder.Configuration.GetSection(
            $"TestOverrides:Resources:{resource.Resource.Name}:Environment"
        );
        foreach (var entry in overrides.GetChildren())
        {
            if (!string.IsNullOrWhiteSpace(entry.Value))
            {
                resource.WithEnvironment(entry.Key, entry.Value);
            }
        }
        return resource;
    }
}
