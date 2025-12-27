using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HraBot.Core;

public class Message
{
    public required int Id { get; init; }
    public required string Content { get; init; }
    public required Role Role { get; init; }
    public required long ConversationId { get; init; }
    public Conversation? Conversation { get; }
}

public enum Role : byte
{
    Undefined = 0,
    System,
    Ai,
    User,
}

public class MessageConfig : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(n => new { n.Id, n.ConversationId });
        builder.Property(n => n.Id).ValueGeneratedNever();

        builder.Property(e => e.Content).HasMaxLength(2000);

        builder
            .HasOne(e => e.Conversation)
            .WithMany(e => e.Messages)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
