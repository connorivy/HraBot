using HraBot.ServiceDefaults;
using Microsoft.Extensions.Configuration;

namespace HraBot.AppHost;

public static class IResouceBuilderExtensions
{
    public static IResourceBuilder<TResouce> ApplyTestEnvironmentOverrides<TResouce>(
        this IResourceBuilder<TResouce> resource
    )
        where TResouce : IResourceWithEnvironment
    {
        ApplyOverridesForService(resource, AppServices.ALL_SERVICES);
        ApplyOverridesForService(resource, resource.Resource.Name);
        return resource;
    }

    private static void ApplyOverridesForService<TResouce>(
        IResourceBuilder<TResouce> resource,
        string serviceName
    )
        where TResouce : IResourceWithEnvironment
    {
        var overrides = resource.ApplicationBuilder.Configuration.GetSection(
            $"TestOverrides:Resources:{serviceName}:Environment"
        );
        foreach (var entry in overrides.GetChildren())
        {
            if (!string.IsNullOrWhiteSpace(entry.Value))
            {
                resource.WithEnvironment(entry.Key, entry.Value);
            }
        }
    }
}
