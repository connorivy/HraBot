using Microsoft.EntityFrameworkCore;

namespace HraBot.Core;

public class HraBotDbContext(DbContextOptions<HraBotDbContext> options) : DbContext(options)
{
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Message> Messages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddEntityConfigurations();
    }
}

public class UnitOfWork(HraBotDbContext dbContext)
{
    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return dbContext.SaveChangesAsync(ct);
    }
}
