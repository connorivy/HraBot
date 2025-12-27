using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceScan.SourceGenerator;

namespace HraBot.Core;

public static partial class Di_Core
{
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
