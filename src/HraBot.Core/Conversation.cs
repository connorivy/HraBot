using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HraBot.Core;

public class Conversation
{
    public long Id { get; private set; }

    public int NumMessages { get; private set; }

    public Message AddTrackedMessage(Role role, string content)
    {
        if (this.Messages is null)
        {
            throw new InvalidOperationException("Messages prop is null on conversation object");
        }
        Message message = new()
        {
            Sequence = ++NumMessages,
            ConversationId = Id,
            Role = role,
            Content = content,
        };
        this.Messages.Add(message);
        return message;
    }

    public List<Message>? Messages { get; set; }

    public DateTimeOffset UpdatedAt { get; private set; }
}

public class ConversationConfig : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedOnAdd();
        builder
            .Property(e => e.UpdatedAt)
            .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'")
            .IsConcurrencyToken()
            .ValueGeneratedOnAddOrUpdate();
    }
}
