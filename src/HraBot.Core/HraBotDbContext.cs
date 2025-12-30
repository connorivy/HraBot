using HraBot.Core.Features.Feedback;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HraBot.Core;

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
public class HraBotDbContext : DbContext
{
    public HraBotDbContext(DbContextOptions<HraBotDbContext> options)
        : base(options) { }

    public HraBotDbContext() { }
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

    public DbSet<Conversation> Conversations { get; set; } = null!;
    public DbSet<Message> Messages { get; set; } = null!;
    public DbSet<MessageFeedback> MessageFeedbacks { get; set; } = null!;
    public DbSet<MessageFeedbackItem> MessageFeedbackItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddEntityConfigurations();
    }

    // protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    // {
    //     optionsBuilder.UseNpgsql();
    // }
}

public class UnitOfWork(HraBotDbContext dbContext)
{
    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return dbContext.SaveChangesAsync(ct);
    }
}

public class BloggingContextFactory : IDesignTimeDbContextFactory<HraBotDbContext>
{
    public HraBotDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<HraBotDbContext>();
        optionsBuilder.UseNpgsql("");
        // optionsBuilder.UseModel(HraBotDbContextModel.Instance);

        return new HraBotDbContext(optionsBuilder.Options);
    }
}
