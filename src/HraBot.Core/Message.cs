using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HraBot.Core;

public class Message
{
    public long Id { get; init; }
    public required int Sequence { get; init; }
    public required string Content { get; init; }
    public virtual required Role Role { get; init; }
    public required long ConversationId { get; init; }
    public Conversation? Conversation { get; }
    public AiModel? AiModel { get; init; }
}

// public class AiMessage : Message
// {
//     public override required Role Role
//     {
// #pragma warning disable CS9266 // Property accessor should use 'field' because the other accessor is using it.
//         get => Role.Ai;
// #pragma warning restore CS9266 // Property accessor should use 'field' because the other accessor is using it.
//         init;
//     }

//     public required AiModel AiModel { get; init; }
// }

public enum Role : byte
{
    Undefined = 0,
    System,
    Ai,
    User,
}

public enum AiModel : byte
{
    Undefined = 0,
    gpt_4o_mini,
    phi_4,
}

public static class AiModelExtensions
{
    public static string ModelString(this AiModel model) =>
        model switch
        {
            _ => throw new NotImplementedException($"Model {model} does not have a model string"),
        };
}

public class MessageConfig : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(n => n.Id);
        builder.HasIndex(n => new { n.ConversationId, n.Sequence }).IsUnique();

        builder.Property(e => e.Content).HasMaxLength(2000);

        builder
            .HasOne(e => e.Conversation)
            .WithMany(e => e.Messages)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
